using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealtimeConsumer;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var eventHubConnectionString = builder.Configuration["TrackingEventHubConnection"];
var eventHubName = builder.Configuration["TrackingEventHubName"];
var consumerGroup = builder.Configuration["TrackingEventHubConsumerGroup"];
var checkpointStorageConnectionString = builder.Configuration["TrackingCheckpointStorageConnectionString"];
var checkpointContainerName = builder.Configuration["TrackingCheckpointContainer"];
var redisConnectionString = builder.Configuration["AzureRedisConnectionString"];

builder.Services.AddSingleton(_ => new BlobContainerClient(
	checkpointStorageConnectionString,
	checkpointContainerName));

builder.Services.AddSingleton(serviceProvider => new EventProcessorClient(
	serviceProvider.GetRequiredService<BlobContainerClient>(),
	consumerGroup,
	eventHubConnectionString,
	eventHubName,
	new EventProcessorClientOptions
	{
		MaximumWaitTime = TimeSpan.FromSeconds(1),
		PrefetchCount = 600
	}));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<RedisLocationStore>();

builder.Services.AddSingleton<SignalRPublisher>();

builder.Services.AddHostedService<TrackingEventProcessor>();

await builder.Build().RunAsync();
