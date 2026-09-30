using System.ComponentModel.DataAnnotations;

namespace TrackingApi.Tracking;

public record LocationUpdate
{
    [Required]
    public Guid EventId { get; init; }

    [Required]
    public string CourierId { get; init; }

    [Required]
    public string DeliveryId { get; init; }

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}