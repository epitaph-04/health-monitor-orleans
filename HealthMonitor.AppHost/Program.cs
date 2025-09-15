var builder = DistributedApplication.CreateBuilder(args);

var redis = builder
    .AddRedis("orleans-redis")
    .WithRedisInsight();

var orleans = builder
    .AddOrleans("cluster")
    .WithClustering(redis)
    .WithGrainStorage("Default", redis)
    .WithReminders(redis);

builder.AddProject<Projects.HealthMonitor>("backend")
    .WithReference(orleans)
    .WaitFor(redis)
    .WithReplicas(1);

builder.Build().Run();