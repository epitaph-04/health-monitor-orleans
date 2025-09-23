using System.Net.Http.Json;
using HealthMonitor.Model;
using Microsoft.AspNetCore.Components;

namespace HealthMonitor.Client.Service;

public class DashboardService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(HttpClient httpClient, NavigationManager manager, ILogger<DashboardService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(manager.BaseUri);
        _logger = logger;
    }

    public async Task<SystemHealthOverview> GetSystemOverviewAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<SystemHealthOverview>("/api/healthtrends/system/overview");
            return response ?? new SystemHealthOverview();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system overview");
            return new SystemHealthOverview();
        }
    }

    public async Task<HealthTrendData> GetServiceTrendAsync(string serviceId, int hours = 24)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<HealthTrendData>($"/api/healthtrends/service/{serviceId}?hours={hours}");
            return response ?? new HealthTrendData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service trend for {ServiceId}", serviceId);
            return new HealthTrendData();
        }
    }

    public async Task<List<HealthTrendData>> GetServiceTrendHistoryAsync(string serviceId, int count = 24)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<HealthTrendData>>($"/api/healthtrends/service/{serviceId}/history?count={count}");
            return response ?? new List<HealthTrendData>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service trend history for {ServiceId}", serviceId);
            return new List<HealthTrendData>();
        }
    }

    public async Task<HealthTrendComparisonReport> CompareServicesAsync(List<string> serviceIds, int hours = 24)
    {
        try
        {
            var request = new CompareServicesRequest { ServiceIds = serviceIds, Hours = hours };
            var response = await _httpClient.PostAsJsonAsync("/api/healthtrends/compare", request);
            return await response.Content.ReadFromJsonAsync<HealthTrendComparisonReport>() ?? new HealthTrendComparisonReport();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare services");
            return new HealthTrendComparisonReport();
        }
    }

    public async Task RefreshAllTrendsAsync()
    {
        try
        {
            await _httpClient.PostAsync("/api/healthtrends/refresh", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh trends");
        }
    }

    public async Task<List<HealthMonitor.Model.Service>> GetAllServices()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<HealthMonitor.Model.Service>>($"/api/healthtrends/services");
            return response ?? new List<HealthMonitor.Model.Service>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get services");
            return new List<HealthMonitor.Model.Service>();
        }
    }
}