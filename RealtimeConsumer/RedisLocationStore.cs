using StackExchange.Redis;
using System.Text.Json;

namespace RealtimeConsumer;

public class RedisLocationStore(IConnectionMultiplexer redis)
{
    private const string UpdateIfNewerScript = "my redis update script";

    public async Task UpdateCurrentLocationsAsync(
        IEnumerable<LocationUpdate> updates,
        CancellationToken cancellationToken)
    {
        var database = redis.GetDatabase();

        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var occurredAtUtcTicks = update.OccurredAt.ToString();
            var serializedLocation = JsonSerializer.Serialize(update);
            var key = new RedisKey($"courier:{update.CourierId}:location");

            await database.ScriptEvaluateAsync(UpdateIfNewerScript, [key], [occurredAtUtcTicks, serializedLocation]);
        }
    }
}