using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace RealtimeConsumer;

public class SignalRPublisher : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly Lazy<Task<IServiceHubContext>> _hubContext;

    public SignalRPublisher(IConfiguration configuration)
    {
        _connectionString = configuration["AzureSignalRConnectionString"];
        _hubContext = new Lazy<Task<IServiceHubContext>>(CreateHubContextAsync);
    }

    public async Task PublishAsync(
        IEnumerable<LocationUpdate> updates,
        CancellationToken cancellationToken)
    {
        var hubContext = await _hubContext.Value;
        foreach (var update in updates)
        {
            await hubContext.Clients
                .Group($"delivery:{update.DeliveryId}")
                .SendAsync("location", update, cancellationToken);
        }
    }

    private async Task<IServiceHubContext> CreateHubContextAsync()
    {
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(options => options.ConnectionString = _connectionString)
            .BuildServiceManager();

        return await serviceManager.CreateHubContextAsync("tracking", CancellationToken.None);
    }

    // this is an unmanaged resource, so needs to be disposed of manually
    public async ValueTask DisposeAsync()
    {
        if (_hubContext.IsValueCreated)
        {
            await (await _hubContext.Value).DisposeAsync();
        }
    }
}