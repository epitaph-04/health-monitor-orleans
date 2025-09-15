using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public enum ServiceType
{
    [Id(0)]
    Http,
    [Id(1)]
    Db,
    [Id(2)]
    Sns,
    [Id(3)]
    Sqs,
    [Id(4)]
    Rabbitmq,
    [Id(5)]
    Certificate,
    [Id(6)]
    Resource,
    [Id(7)]
    Network
}