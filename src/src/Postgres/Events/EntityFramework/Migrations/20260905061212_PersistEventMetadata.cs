using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Purview.EventSourcing.Postgres.Events.EntityFramework.Migrations;

/// <summary>Adds independently queryable event metadata columns.</summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("20260905061212_PersistEventMetadata")]
public sealed class PersistEventMetadata : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		ArgumentNullException.ThrowIfNull(migrationBuilder);

		migrationBuilder.AddColumn<string>(
			name: "CausationId",
			schema: "public",
			table: "EventStoreEvents",
			type: "character varying(450)",
			maxLength: 450,
			nullable: true
		);
		migrationBuilder.AddColumn<string>(
			name: "CorrelationId",
			schema: "public",
			table: "EventStoreEvents",
			type: "character varying(450)",
			maxLength: 450,
			nullable: true
		);
		migrationBuilder.AddColumn<int>(
			name: "SchemaVersion",
			schema: "public",
			table: "EventStoreEvents",
			type: "integer",
			nullable: false,
			defaultValue: 1
		);
		migrationBuilder.AddColumn<string>(
			name: "UserId",
			schema: "public",
			table: "EventStoreEvents",
			type: "character varying(450)",
			maxLength: 450,
			nullable: true
		);
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		ArgumentNullException.ThrowIfNull(migrationBuilder);

		migrationBuilder.DropColumn("CausationId", "public", "EventStoreEvents");
		migrationBuilder.DropColumn("CorrelationId", "public", "EventStoreEvents");
		migrationBuilder.DropColumn("SchemaVersion", "public", "EventStoreEvents");
		migrationBuilder.DropColumn("UserId", "public", "EventStoreEvents");
	}
}
