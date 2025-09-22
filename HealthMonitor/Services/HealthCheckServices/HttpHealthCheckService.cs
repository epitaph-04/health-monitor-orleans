using System.Diagnostics;
using System.Text;
using HealthMonitor.Model;

namespace HealthMonitor.Services.HealthCheckServices;

public class HttpHealthCheckService : IHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHealthCheckService> _logger;
    public HttpHealthCheckService(HttpClient httpClient, ServiceConfiguration serviceConfiguration, ILogger<HttpHealthCheckService> logger)
    {
        ServiceConfiguration = serviceConfiguration;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(serviceConfiguration.TimeoutSeconds);
        _logger = logger;
    }
    
    public ServiceConfiguration ServiceConfiguration { get; }

    public async ValueTask<HealthCheckRecord> CheckHealthAsync()
    {
        var result = new HealthCheckRecord();
        var stopwatch = new Stopwatch();

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(ServiceConfiguration.Method), ServiceConfiguration.Target);
            if (ServiceConfiguration.Headers != null)
            {
                foreach (var header in ServiceConfiguration.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            if (!string.IsNullOrEmpty(ServiceConfiguration.RequestBody) && (ServiceConfiguration.Method.ToUpper() == "POST" || ServiceConfiguration.Method.ToUpper() == "PUT"))
            {
                string contentType = "application/json";
                if(ServiceConfiguration.Headers != null && ServiceConfiguration.Headers.TryGetValue("Content-Type", out var ctHeader))
                {
                    contentType = ctHeader;
                }
                request.Content = new StringContent(ServiceConfiguration.RequestBody, Encoding.UTF8, contentType);
            }

            stopwatch.Start();
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            stopwatch.Stop();

            result.ResponseTime = stopwatch.Elapsed;
            result.Status = response.StatusCode == (System.Net.HttpStatusCode)ServiceConfiguration.ExpectedResponseCode
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
            result.ErrorMessage = $"Request timed out after {ServiceConfiguration.TimeoutSeconds} seconds. {ex.Message}";
            result.ResponseTime = stopwatch.Elapsed;
            _logger.LogError("Request timed out, {error}", ex);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            result.Status = Status.Critical;
            result.ErrorMessage = $"HTTP request failed: {ex.Message}";
            if(stopwatch.IsRunning) stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            _logger.LogError("HTTP request failed, {error}", ex);
        }
        catch (Exception ex)
        {
            if(stopwatch.IsRunning) stopwatch.Stop();
            result.Status = Status.Critical;
            result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
            result.ResponseTime = stopwatch.Elapsed;
            _logger.LogError("An unexpected error occurred, {error}", ex);
        }
        finally
        {
            result.Timestamp = DateTime.UtcNow;
        }
        return result;
    }
}