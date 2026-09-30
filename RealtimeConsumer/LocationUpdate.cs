namespace RealtimeConsumer;

public record LocationUpdate(
    Guid EventId,
    string CourierId,
    string DeliveryId,
    double Latitude,
    double Longitude,
    DateTimeOffset OccurredAt);