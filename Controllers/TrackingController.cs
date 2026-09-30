using Microsoft.AspNetCore.Mvc;
using TrackingApi.Tracking;

namespace TrackingApi.Controllers;

[ApiController]
[Route("api/tracking")]
public sealed class TrackingController(ITrackingEventPublisher publisher) : ControllerBase
{
    [HttpPost("location")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostLocation(
        [FromBody] LocationUpdate update,
        CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(update, cancellationToken);
        return Accepted(new { update.EventId });
    }
}