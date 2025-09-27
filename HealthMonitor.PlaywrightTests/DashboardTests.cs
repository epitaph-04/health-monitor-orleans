using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace HealthMonitor.PlaywrightTests;

public class DashboardTests : IAsyncLifetime
{
    private const string BaseUrl = "https://localhost:7001"; // Adjust port as needed
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    protected IPage Page => _page ?? throw new InvalidOperationException("Page not initialized");

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var context = await _browser.NewContextAsync();
        _page = await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task Dashboard_ShouldLoad_Successfully()
    {
        // Navigate to the dashboard
        await Page.GotoAsync(BaseUrl);

        // Wait for the page to load and check title
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check that the page has loaded
        var title = await Page.TitleAsync();
        Assert.False(string.IsNullOrEmpty(title));

        // Check for main dashboard elements
        await Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Dashboard_ShouldDisplay_ServiceCards()
    {
        // Navigate to the dashboard
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait a bit for the SignalR connection and data loading
        await Page.WaitForTimeoutAsync(2000);

        // Look for service cards or loading indicators
        var hasServiceCards = await Page.Locator(".service-card, .mud-card, [data-testid='service-card']").CountAsync() > 0;
        var hasLoadingIndicator = await Page.Locator(".mud-progress-circular, .loading, [data-testid='loading']").IsVisibleAsync();

        // Either should have service cards or show loading
        Assert.True(hasServiceCards || hasLoadingIndicator,
            "Dashboard should either show service cards or loading indicator");
    }

    [Fact]
    public async Task Dashboard_ShouldHandle_RefreshButton()
    {
        // Navigate to the dashboard
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for refresh button (common selectors)
        var refreshButton = Page.Locator("button:has-text('Refresh'), .refresh-button, [data-testid='refresh-button']");

        // If refresh button exists, click it
        if (await refreshButton.CountAsync() > 0)
        {
            await refreshButton.First.ClickAsync();

            // Wait for potential loading state after refresh
            await Page.WaitForTimeoutAsync(1000);

            // Verify page is still responsive
            await Expect(Page.Locator("body")).ToBeVisibleAsync();
        }
        else
        {
            // If no refresh button found, that's okay - just log it
            Console.WriteLine("No refresh button found on dashboard");
        }
    }

    [Fact]
    public async Task Dashboard_ShouldHandle_NavigationMenu()
    {
        // Navigate to the dashboard
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for navigation elements (MudBlazor drawer, nav menu, etc.)
        var navElements = await Page.Locator(
            ".mud-drawer, .mud-nav-link, .nav-menu, nav, [data-testid='nav-menu']"
        ).CountAsync();

        if (navElements > 0)
        {
            // Try to interact with navigation if it exists
            var navLinks = Page.Locator("a[href], .mud-nav-link");
            var linkCount = await navLinks.CountAsync();

            if (linkCount > 0)
            {
                // Get the first navigation link
                var firstLink = navLinks.First;
                var href = await firstLink.GetAttributeAsync("href");

                // Only click if it's a relative link (not external)
                if (!string.IsNullOrEmpty(href) && (href.StartsWith("/") || href.StartsWith(".")))
                {
                    await firstLink.ClickAsync();
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                    // Verify navigation worked
                    await Expect(Page.Locator("body")).ToBeVisibleAsync();
                }
            }
        }
    }

    [Fact]
    public async Task Dashboard_ShouldDisplay_HealthStatus()
    {
        // Navigate to the dashboard
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for data to load
        await Page.WaitForTimeoutAsync(3000);

        // Look for health status indicators
        var healthIndicators = await Page.Locator(
            ".health-status, .status-healthy, .status-critical, .status-warning, " +
            ".mud-chip-color-success, .mud-chip-color-error, .mud-chip-color-warning, " +
            "[data-testid='health-status']"
        ).CountAsync();

        var hasCharts = await Page.Locator(
            ".apexcharts-canvas, canvas, svg, .chart, [data-testid='chart']"
        ).CountAsync() > 0;

        var hasMetrics = await Page.Locator(
            ".metric, .health-metric, .availability, .response-time, " +
            "[data-testid='metric']"
        ).CountAsync() > 0;

        // Should have at least one of: health indicators, charts, or metrics
        Assert.True(healthIndicators > 0 || hasCharts || hasMetrics,
            "Dashboard should display health status, charts, or metrics");
    }

    [Fact]
    public async Task Dashboard_ShouldBeResponsive()
    {
        // Test desktop size
        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("body")).ToBeVisibleAsync();

        // Test tablet size
        await Page.SetViewportSizeAsync(768, 1024);
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("body")).ToBeVisibleAsync();

        // Test mobile size
        await Page.SetViewportSizeAsync(375, 667);
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Dashboard_ShouldHandle_ErrorStates()
    {
        // Navigate to dashboard
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for potential error messages or loading states
        await Page.WaitForTimeoutAsync(5000);

        // Check for error indicators
        var hasErrors = await Page.Locator(
            ".error, .alert-error, .mud-alert-error, .error-message, " +
            "[data-testid='error'], .text-danger"
        ).CountAsync();

        var hasContent = await Page.Locator(
            ".service-card, .mud-card, .chart, .metric, .dashboard-content, " +
            "[data-testid='dashboard-content']"
        ).CountAsync();

        // Either should have content or gracefully handle no data
        var pageText = await Page.TextContentAsync("body");
        var hasGracefulMessage = pageText?.Contains("No services") == true ||
                                pageText?.Contains("Loading") == true ||
                                pageText?.Contains("loading") == true;

        Assert.True(hasContent > 0 || hasGracefulMessage,
            "Dashboard should either show content or graceful no-data message");
    }

    // Note: Run 'playwright install chromium' before running tests
}