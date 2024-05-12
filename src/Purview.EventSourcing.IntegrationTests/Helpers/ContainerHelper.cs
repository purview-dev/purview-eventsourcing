using System.Reflection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.Azurite;
using Testcontainers.CosmosDb;
using Testcontainers.MongoDb;

namespace Purview.EventSourcing;

public static class ContainerHelper
{
	public static AzuriteContainer CreateAzurite(Action<AzuriteBuilder>? config = null)
	{
		var builder = new AzuriteBuilder()
			.WithImage("mcr.microsoft.com/azure-storage/azurite:3.30.0")
			.WithWaitStrategy(Wait.ForUnixContainer()
				.UntilPortIsAvailable(10000) // Blob
				.UntilPortIsAvailable(10001) // Queue
				.UntilPortIsAvailable(10002) // Table
			)
		;

		config?.Invoke(builder);

		return builder.Build();
	}

	public static CosmosDbContainer CreateCosmosDB(Action<CosmosDbBuilder>? config = null)
	{
		var builder = new CosmosDbBuilder()
			.WithAutoRemove(true)
			.WithCleanUp(true)
			//.WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "5")
			//.WithEnvironment("AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE", "127.0.0.1")
			.WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "false")
			.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8081))
		;

		config?.Invoke(builder);

		return builder.Build();
	}

	public static MongoDbContainer CreateMongoDB(Action<MongoDbBuilder>? config = null)
	{
		var builder = new MongoDbBuilder()
			//.WithAutoRemove(true)
			.WithCleanUp(true)
			.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
		;

		config?.Invoke(builder);

		return builder.Build();
	}

	public static IContainer CreateMongoDBWithReplicaSet(Action<ContainerBuilder>? config = null)
	{
		var mntPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources/mongodb/");

		var builder = new ContainerBuilder()
			.WithImage("mongo:7.0")
			.WithCommand("--replSet rs0 --bind_ip_all")
			.WithPortBinding(MongoDbBuilder.MongoDbPort, true)
			//.WithBindMount(mntPath, "/data-keys/")
			.WithEnvironment("MONGO_INITDB_ROOT_USERNAME", MongoDbBuilder.DefaultUsername)
			.WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", MongoDbBuilder.DefaultPassword)
			.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MongoDbBuilder.MongoDbPort))
			.WithStartupCallback(async (container, cancellationToken) =>
			{
				// test: echo "try { rs.status() } catch (err) { rs.initiate({_id:'rs0',members:[{_id:0,host:'host.docker.internal:27017'}]}) }" | mongosh --port 27017 --quiet


				//await container.ExecAsync(["apt-get update -y", "apt-get upgrade -y", "apt-get install openssl -y"], e =>
				//{
				//	return false;
				//}, cancellationToken: cancellationToken);

				//, 
				//await container.ExecAsync(["openssl rand -base64 756 > repl-key;"], e =>
				//{
				//	return false;
				//}, cancellationToken: cancellationToken);

				await container.ExecAsync(["bash", "-c", $"echo 'disableTelemetry()' | mongosh -u $MONGO_INITDB_ROOT_USERNAME -p $MONGO_INITDB_ROOT_PASSWORD"], cancellationToken: cancellationToken);

				await container.ExecAsync(["bash", "-c", $"echo 'try {{ rs.status() }} catch (err) {{ rs.initiate({{_id: \"rs0\", members: [{{ _id: 0, host: \"localhost:{MongoDbBuilder.MongoDbPort}\" }}]}}) }};' | mongosh -u $MONGO_INITDB_ROOT_USERNAME -p $MONGO_INITDB_ROOT_PASSWORD"], e =>
				{
					return false;
				}, cancellationToken: cancellationToken);

				return;

				//  /data/keys/
				await container.ExecAsync(["ls /data/"], e =>
				{
					return false;
				}, cancellationToken: cancellationToken);

				await container.ExecAsync(["chmod 400 /data/keys/repl.key"], e =>
				{
					return false;
				}, cancellationToken: cancellationToken);

				await container.ExecAsync(["bash", "-c", $"echo 'disableTelemetry(); rs.initiate({{_id: \"rs0\", members: [{{ _id: 0, host: \"localhost:{MongoDbBuilder.MongoDbPort}\" }}]}});' | mongosh --keyFile /data/keys/repl.key -u $MONGO_INITDB_ROOT_USERNAME -p $MONGO_INITDB_ROOT_PASSWORD"], e =>
				{
					return false;
				}, cancellationToken: cancellationToken);

				//var settings = MongoClientSettings.FromUrl(new MongoUrl($"mongodb://{MongoDbBuilder.DefaultUsername}:{MongoDbBuilder.DefaultPassword}@localhost:{container.GetMappedPublicPort(MongoDbBuilder.MongoDbPort)}"));
				//settings.ReplicaSetName = "rs0";

				//MongoClient mongoClient = new(settings);
				//BsonDocumentCommand<BsonDocument> databaseCmd = new(new()
				//{
				//	{"replSetInitiate", new BsonDocument()}
				//});

				//var database = mongoClient.GetDatabase("admin");

				//var attempts = 0;
				//while (attempts < 10)
				//{
				//	try
				//	{
				//		//await database.RunCommandAsync(databaseCmd, cancellationToken: cancellationToken);
				//		var result = await container.ExecAsync(["bash", "-c", $"echo 'disableTelemetry(); rs.initiate({{_id: \"rs0\", members: [{{ _id: 0, host: \"localhost:{MongoDbBuilder.MongoDbPort}\" }}]}});' | mongosh --keyFile repl-key -u $MONGO_INITDB_ROOT_USERNAME -p $MONGO_INITDB_ROOT_PASSWORD"], cancellationToken);
				//		var exitCode = result.ExitCode;

				//		if (exitCode == 0)
				//		{
				//			break;
				//		}

				//		exitCode.Should().Be(0,
				//			because: $"MongoDB replica set initialization failed. Attempt {attempts + 1} of 10. Stdout: {result.Stdout}, Stderr: {result.Stderr}");
				//	}
				//	catch
				//	{
				//		await Task.Delay(250, cancellationToken);
				//		attempts++;
				//	}
				//}

				//await database.RunCommandAsync(databaseCmd, cancellationToken: cancellationToken);

				//await container.ExecAsync(["bash", "-c", "echo 'rs.initiate({_id: \"rs0\", members: [{ _id: 0, host: \"localhost:27017\" }]});' | mongo"], cancellationToken);
			});

		config?.Invoke(builder);

		return builder.Build();

		static bool IsReplicaSetInitialized(string connectionString)
		{
			var client = new MongoClient(connectionString);
			var adminDb = client.GetDatabase("admin");
			var replSetGetStatus = new BsonDocumentCommand<BsonDocument>(new() { { "replSetGetStatus", 1 } });
			var result = adminDb.RunCommand(replSetGetStatus);

			return result["ok"] == 1;
		}
	}

	static Task<bool> ExecAsync(this IContainer container, string command, Func<ExecResult, bool>? isSuccess = null, bool throwOnFailure = true, int attempts = 10, int delayInMS = 100, CancellationToken cancellationToken = default)
		=> ExecAsync(container, new List<string> { command }, isSuccess, throwOnFailure, attempts, delayInMS, cancellationToken);

	static async Task<bool> ExecAsync(this IContainer container, List<string> commands, Func<ExecResult, bool>? isSuccess = null, bool throwOnFailure = true, int attempts = 10, int delayInMS = 100, CancellationToken cancellationToken = default)
	{
		var attemptCount = 0;
		while (attemptCount < attempts)
		{
			try
			{
				var result = await container.ExecAsync(commands, cancellationToken);
				if (result.ExitCode == 0)
				{
					return isSuccess is not null && !isSuccess(result)
						? throw new Exception($"Command failed. Stdout: {result.Stdout}, Stderr: {result.Stderr}")
						: true;
				}

				result.ExitCode.Should().Be(0,
					because: $"MongoDB replica set initialization failed. Attempt {attemptCount + 1} of 10. Stdout: {result.Stdout}, Stderr: {result.Stderr}");
			}
			catch
			{
				await Task.Delay(delayInMS, cancellationToken);
				attemptCount++;
			}
		}

		if (throwOnFailure)
			throw new Exception("Failed to execute command.");

		return false;
	}
}
