using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public enum ServiceType
{
    Http,
    Db,
    Sns,
    Sqs,
    Rabbitmq,
    Certificate,
    Resource,
    Network
}