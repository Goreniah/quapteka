using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace TrackingApi.Tracking;

public class EventHubTrackingEventPublisher : ITrackingEventPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producer;

    public EventHubTrackingEventPublisher(IConfiguration configuration)
    {
        var connectionString = configuration["TrackingEventHub:ConnectionString"];
        var eventHubName = configuration["TrackingEventHub:EventHubName"];

        _producer = new EventHubProducerClient(connectionString, eventHubName);
    }

    public async Task PublishAsync(LocationUpdate update, CancellationToken cancellationToken)
    {
        var payload = new
        {
            update.EventId,
            update.CourierId,
            update.DeliveryId,
            update.Latitude,
            update.Longitude,
            update.OccurredAt
        };

        using var batch = await _producer.CreateBatchAsync(new CreateBatchOptions { PartitionKey = update.CourierId }, cancellationToken);

        if (!batch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(payload))))
        {
            throw new InvalidOperationException("Meaningful error message.");
        }

        await _producer.SendAsync(batch, cancellationToken);
    }

    // unmanaged resource, needs to be disposed of manually
    public ValueTask DisposeAsync() => _producer.DisposeAsync();
}