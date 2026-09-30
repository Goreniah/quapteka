using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Hosting;

namespace RealtimeConsumer;

public class TrackingEventProcessor(
    EventProcessorClient eventProcessor,
    BlobContainerClient checkpointContainer,
    RedisLocationStore redisLocationStore,
    SignalRPublisher signalrPublisher) : BackgroundService
{
    // locking is needed because event hub can spawn multiple threads to execute callbacks
    private readonly object _pendingLock = new();
    private readonly Dictionary<string, LocationUpdate> _latestByDelivery = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ProcessEventArgs> _latestCheckpointByPartition = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await checkpointContainer.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        eventProcessor.ProcessEventAsync += ProcessEventAsync;
        // ignored ProcessErrorAsync, but it should be defined in prod code

        await eventProcessor.StartProcessingAsync(stoppingToken);

        // coalescing updates not to overload redis and signalr
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushPending(stoppingToken);
        }

        await eventProcessor.StopProcessingAsync(CancellationToken.None);
        await FlushPending(CancellationToken.None);
    }

    private Task ProcessEventAsync(ProcessEventArgs args)
    {
        var update = JsonSerializer.Deserialize<LocationUpdate>(args.Data.EventBody);

        lock (_pendingLock)
        {
            _latestByDelivery[update.DeliveryId] = update;
            _latestCheckpointByPartition[args.Partition.PartitionId] = args;
        }

        return Task.CompletedTask;
    }

    private async Task FlushPending(CancellationToken cancellationToken)
    {
        Dictionary<string, LocationUpdate> updates;
        Dictionary<string, ProcessEventArgs> checkpoints;

        lock (_pendingLock)
        {
            // copying the dictionaries to make the lock as short in time as possible
            updates = new Dictionary<string, LocationUpdate>(_latestByDelivery);
            checkpoints = new Dictionary<string, ProcessEventArgs>(_latestCheckpointByPartition);
        }

        if (updates.Count > 0)
        {
            var batch = updates.Values.ToArray();

            await redisLocationStore.UpdateCurrentLocationsAsync(batch, cancellationToken);
            await signalrPublisher.PublishAsync(batch, cancellationToken);
        }

        // update checkpoints only after processing the location updates
        foreach (var (partitionId, checkpoint) in checkpoints)
        {
            await checkpoint.UpdateCheckpointAsync(cancellationToken);

            lock (_pendingLock)
            {
                _latestCheckpointByPartition.Remove(partitionId);
            }
        }

        lock (_pendingLock)
        {
            foreach (var (deliveryId, published) in updates)
            {
                _latestByDelivery.Remove(deliveryId);
            }
        }
    }
}