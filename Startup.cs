using TrackingApi.Tracking;

namespace TrackingApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddSingleton<ITrackingEventPublisher, EventHubTrackingEventPublisher>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
    {
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            if (environment.IsDevelopment())
            {
                endpoints.MapOpenApi();
            }

            endpoints.MapControllers();
        });
    }
}