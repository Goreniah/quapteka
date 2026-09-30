namespace TrackingApi.Tracking;

public interface ITrackingEventPublisher
{
    Task PublishAsync(LocationUpdate update, CancellationToken cancellationToken);
}