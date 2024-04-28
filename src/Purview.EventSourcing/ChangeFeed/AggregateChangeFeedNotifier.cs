﻿using System.Diagnostics;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.ChangeFeed;

sealed class AggregateChangeFeedNotifier<T>(
	IAggregateChangeFeedNotifierTelemetry logs,
	IEnumerable<IAggregateChangeFeedProcessor> changeFeedProcessors,
	IEnumerable<IAggregateChangeFeedProcessor<T>> typedChangeFeedProcessors) : IAggregateChangeFeedNotifier<T>
	where T : class, IAggregate, new()
{

	readonly IAggregateChangeFeedProcessor[] _changeFeedProcessors = changeFeedProcessors.ToArray();
	readonly IAggregateChangeFeedProcessor<T>[] _typedChangeFeedProcessors = typedChangeFeedProcessors.ToArray();

	public async Task BeforeDeleteAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		logs.BeforeDeleteNotificationStart(aggregate.Id(), aggregate.AggregateType);

		var sw = Stopwatch.StartNew();

		for (var i = 0; i < _changeFeedProcessors.Length; i++)
		{
			var processor = _changeFeedProcessors[i];
			if (processor.CanProcess(aggregate))
				await ProcessTimedNotifyAsync(() => processor.BeforeDeleteAsync(aggregate, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		for (var i = 0; i < _typedChangeFeedProcessors.Length; i++)
		{
			var processor = _typedChangeFeedProcessors[i];
			await ProcessTimedNotifyAsync(() => processor.BeforeDeleteAsync(aggregate, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		sw.Stop();

		logs.BeforeDeleteNotificationComplete(aggregate.Id(), aggregate.AggregateType, sw.ElapsedMilliseconds);
	}

	public async Task BeforeSaveAsync(T aggregate, bool isNew, CancellationToken cancellationToken = default)
	{
		logs.BeforeSaveNotificationStart(aggregate.Id(), aggregate.AggregateType, isNew);

		var sw = Stopwatch.StartNew();

		for (var i = 0; i < _changeFeedProcessors.Length; i++)
		{
			var processor = _changeFeedProcessors[i];
			if (processor.CanProcess(aggregate))
				await ProcessTimedNotifyAsync(() => processor.BeforeSaveAsync(aggregate, isNew, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		for (var i = 0; i < _typedChangeFeedProcessors.Length; i++)
		{
			var processor = _typedChangeFeedProcessors[i];
			await ProcessTimedNotifyAsync(() => processor.BeforeSaveAsync(aggregate, isNew, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		sw.Stop();

		logs.BeforeSaveNotificationComplete(aggregate.Id(), aggregate.AggregateType, isNew, sw.ElapsedMilliseconds);
	}

	public async Task AfterSaveAsync(T aggregate, int previousSavedVersion, bool isNew, IEvent[] events, CancellationToken cancellationToken = default)
	{
		logs.AfterSaveNotificationStart(aggregate.Id(), aggregate.AggregateType, isNew, events.Length);

		var sw = Stopwatch.StartNew();

		for (var i = 0; i < _changeFeedProcessors.Length; i++)
		{
			var processor = _changeFeedProcessors[i];
			if (processor.CanProcess(aggregate))
				await ProcessTimedNotifyAsync(() => processor.AfterSaveAsync(aggregate, previousSavedVersion, isNew, events, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		for (var i = 0; i < _typedChangeFeedProcessors.Length; i++)
		{
			var processor = _typedChangeFeedProcessors[i];
			await ProcessTimedNotifyAsync(() => processor.AfterSaveAsync(aggregate, previousSavedVersion, isNew, events, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		sw.Stop();

		logs.AfterSaveNotificationComplete(aggregate.Id(), aggregate.AggregateType, isNew, events.Length, sw.ElapsedMilliseconds);
	}

	public async Task AfterDeleteAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		logs.AfterDeleteNotificationStart(aggregate.Id(), aggregate.AggregateType);

		var sw = Stopwatch.StartNew();

		for (var i = 0; i < _changeFeedProcessors.Length; i++)
		{
			var processor = _changeFeedProcessors[i];
			if (processor.CanProcess(aggregate))
				await ProcessTimedNotifyAsync(() => processor.AfterDeleteAsync(aggregate, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		for (var i = 0; i < _typedChangeFeedProcessors.Length; i++)
		{
			var processor = _typedChangeFeedProcessors[i];
			await ProcessTimedNotifyAsync(() => processor.AfterDeleteAsync(aggregate, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		sw.Stop();

		logs.AfterDeleteNotificationComplete(aggregate.Id(), aggregate.AggregateType, sw.ElapsedMilliseconds);
	}

	public async Task FailureAsync(T aggregate, bool isDelete, Exception exception, CancellationToken cancellationToken = default)
	{
		logs.FailureNotificationStart(aggregate.Id(), aggregate.AggregateType, exception);

		var sw = Stopwatch.StartNew();

		for (var i = 0; i < _changeFeedProcessors.Length; i++)
		{
			var processor = _changeFeedProcessors[i];
			if (processor.CanProcess(aggregate))
				await ProcessTimedNotifyAsync(() => processor.FailureAsync(aggregate, isDelete, exception, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		for (var i = 0; i < _typedChangeFeedProcessors.Length; i++)
		{
			var processor = _typedChangeFeedProcessors[i];
			await ProcessTimedNotifyAsync(() => processor.FailureAsync(aggregate, isDelete, exception, cancellationToken), aggregate, processor.GetType().FullName.OrDefault(() => processor.GetType().Name));
		}

		sw.Stop();

		logs.FailureNotificationComplete(aggregate.Id(), aggregate.AggregateType, sw.ElapsedMilliseconds);
	}

	async Task ProcessTimedNotifyAsync(Func<Task> process, T aggregate, string processorType)
	{
		logs.ProcessingStart(aggregate.Id(), processorType);

		var successful = true;
		var sw = Stopwatch.StartNew();

		try
		{
			await process();
		}
		catch (Exception ex)
		{
			successful = false;
			logs.ProcessingFailed(aggregate.Id(), processorType, ex);
		}

		sw.Stop();

		logs.ProcessingComplete(aggregate.Id(), processorType, sw.ElapsedMilliseconds, successful);
	}
}
