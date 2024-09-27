var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddAzureRedis("redis")
    .RunAsContainer(c =>
    {
        c.WithDataVolume()
            .WithRedisCommander()
            .WithRedisInsight(c => c.WithAcceptEula());
    });

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(redis).WaitFor(redis);

builder.Build().Run();
