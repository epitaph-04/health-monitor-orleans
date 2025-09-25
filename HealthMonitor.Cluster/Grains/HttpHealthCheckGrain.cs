using System.Diagnostics;
using System.Text;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Orleans.Providers;

namespace HealthMonitor.Cluster.Grains;

[GenerateSerializer]
public class HealthDataState
{
    [Id(0)]
    public ServiceConfiguration ServiceConfiguration  { get; set; } = new();
    [Id(1)]
    public List<HealthCheckRecord> Records { get; set; } = new();
    [Id(2)]
    public DateTime LastUpdated { get; set; }
}

[StorageProvider(ProviderName = "Default")]
public class HttpHealthCheckGrain(
    HttpClient httpClient, 
    ILogger<HttpHealthCheckGrain> logger
    ) : Grain<HealthDataState>, IHealthCheckGrain, IRemindable
{
    private const int MaxRecordsPerGrain = 129600;
    private const string HealthCheckReminder = "HEALTH_CHECK_REMINDER";
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (State.ServiceConfiguration.Id.IsWhiteSpace())
        {
            State.ServiceConfiguration.Id = this.GetPrimaryKeyString();
            State.Records = new List<HealthCheckRecord>();
        }
        await base.OnActivateAsync(cancellationToken);
    }

    public async ValueTask Register(ServiceConfiguration configuration, CancellationToken token)
    {
        State.ServiceConfiguration = configuration;
        await WriteStateAsync();
        await this.RegisterOrUpdateReminder(
            $"{HealthCheckReminder}-{this.GetPrimaryKeyString()}", 
            TimeSpan.Zero,
            TimeSpan.FromMinutes(configuration.IntervalMinutes));
    }

    public ValueTask<HealthCheckRecord> GetLastRecord(CancellationToken token) 
        => ValueTask.FromResult(State.Records.Any() ? State.Records.Last() : new());
    
    public ValueTask<List<HealthCheckRecord>> GetHealthRecords(DateTime fromTime, DateTime toTime, CancellationToken token)
    {
        var filteredRecords = State.Records
            .Where(r => r.Timestamp >= fromTime && r.Timestamp <= toTime)
            .OrderBy(r => r.Timestamp)
            .ToList();
        
        return ValueTask.FromResult(filteredRecords);
    }

    public ValueTask<List<HealthCheckRecord>> GetRecentRecords(TimeSpan timespan, CancellationToken token)
    {
        var cutoff = DateTime.UtcNow - timespan;
        return GetHealthRecords(cutoff, DateTime.UtcNow, token);
    }

    public ValueTask<HealthDataStatistics> GetStatistics(DateTime fromTime, DateTime toTime, CancellationToken token)
    {
        var records = State.Records.Where(r => r.Timestamp >= fromTime && r.Timestamp <= toTime).ToList();
        
        if (!records.Any())
        {
            return ValueTask.FromResult(new HealthDataStatistics());
        }

        var healthyRecords = records.Where(r => r.Status.IsHealthy()).ToList();
        var responseTimes = records.Where(r => r.ResponseTime > TimeSpan.Zero).Select(r => r.ResponseTime).ToList();

        var stats = new HealthDataStatistics
        {
            TotalRecords = records.Count,
            AvailabilityPercentage = (double)healthyRecords.Count / records.Count * 100,
            FailureCount = records.Count - healthyRecords.Count,
            FirstRecord = records.Min(r => r.Timestamp),
            LastRecord = records.Max(r => r.Timestamp)
        };

        if (responseTimes.Any())
        {
            stats.AverageResponseTime = TimeSpan.FromTicks((long)responseTimes.Average(rt => rt.Ticks));
            stats.MaxResponseTime = responseTimes.Max();
            stats.MinResponseTime = responseTimes.Min();
        }

        return ValueTask.FromResult(stats);
    }

    public ValueTask<int> GetRecordCount(CancellationToken token)
    {
        return ValueTask.FromResult(State.Records.Count);
    }

    public async ValueTask CleanupOldRecords(DateTime cutoffTime, CancellationToken token)
    {
        var originalCount = State.Records.Count;
        State.Records = State.Records.Where(r => r.Timestamp > cutoffTime).ToList();
        
        if (State.Records.Count != originalCount)
        {
            await WriteStateAsync();
            logger.LogInformation("Cleaned up {Count} old records for service {ServiceId}", 
                originalCount - State.Records.Count, State.ServiceConfiguration.Id);
        }
    }
    
    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        var healthCheckRecord = await CheckHealthAsync();
        
        State.Records.Add(healthCheckRecord);
        State.LastUpdated = DateTime.UtcNow;
        
        if (State.Records.Count > MaxRecordsPerGrain)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            State.Records = State.Records.Where(r => r.Timestamp > cutoff).ToList();
        }
        await WriteStateAsync();
    }
    
    private async ValueTask<HealthCheckRecord> CheckHealthAsync()
    {
        var result = new HealthCheckRecord();
        var stopwatch = new Stopwatch();

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(State.ServiceConfiguration.Method), State.ServiceConfiguration.Target);
            if (State.ServiceConfiguration.Headers != null)
            {
                foreach (var header in State.ServiceConfiguration.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            if (!string.IsNullOrEmpty(State.ServiceConfiguration.RequestBody) && (State.ServiceConfiguration.Method.ToUpper() == "POST" || State.ServiceConfiguration.Method.ToUpper() == "PUT"))
            {
                string contentType = "application/json";
                if(State.ServiceConfiguration.Headers != null && State.ServiceConfiguration.Headers.TryGetValue("Content-Type", out var ctHeader))
                {
                    contentType = ctHeader;
                }
                request.Content = new StringContent(State.ServiceConfiguration.RequestBody, Encoding.UTF8, contentType);
            }

            stopwatch.Start();
            HttpResponseMessage response = await httpClient.SendAsync(request);
            stopwatch.Stop();

            result.ResponseTime = stopwatch.Elapsed;
            result.Status = response.StatusCode == (System.Net.HttpStatusCode)State.ServiceConfiguration.ExpectedResponseCode
                ? Status.Healthy
                : Status.Critical;
            if (result.Status == Status.Critical)
            {
                result.ErrorMessage = $"Unexpected status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            result.Status = Status.Critical;
            result.ErrorMessage = $"Request timed out after {State.ServiceConfiguration.TimeoutSeconds} seconds. {ex.Message}";
            result.ResponseTime = stopwatch.Elapsed;
            logger.LogError("Request timed out, {error}", ex);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            result.Status = Status.Critical;
            result.ErrorMessage = $"HTTP request failed: {ex.Message}";
            if(stopwatch.IsRunning) stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            logger.LogError("HTTP request failed, {error}", ex);
        }
        catch (Exception ex)
        {
            if(stopwatch.IsRunning) stopwatch.Stop();
            result.Status = Status.Critical;
            result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
            result.ResponseTime = stopwatch.Elapsed;
            logger.LogError("An unexpected error occurred, {error}", ex);
        }
        finally
        {
            result.Timestamp = DateTime.UtcNow;
        }
        return result;
    }
}