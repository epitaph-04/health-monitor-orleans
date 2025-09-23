var builder = DistributedApplication.CreateBuilder(args);

var redis = builder
    .AddRedis("orleans-redis")
    .WithRedisInsight();

var orleans = builder
    .AddOrleans("cluster")
    .WithClustering(redis)
    .WithGrainStorage("Default", redis)
    .WithReminders(redis);

var healthMonitorCluster = builder
    .AddProject<Projects.HealthMonitor_Cluster>("health-monitor-cluster")
    .WithReference(orleans)
    .WaitFor(redis)
    .WithReplicas(2);

builder
    .AddProject<Projects.HealthMonitor_Cluster_Dashboard>("health-monitor-cluster-dashboard")
    .WithReference(orleans)
    .WaitFor(redis)
    .WithReplicas(1);

builder
    .AddProject<Projects.HealthMonitor>("health-monitor")
    .WithReference(orleans.AsClient())
    .WaitFor(healthMonitorCluster)
    .WithReplicas(1);

builder.Build().Run();