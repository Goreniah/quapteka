var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<TrackingApi.Startup>())
    .Build();

host.Run();
