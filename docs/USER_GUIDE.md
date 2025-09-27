# Health Monitor User Guide

This guide walks you through using the Health Monitor Orleans system, covering both the Dashboard for real-time monitoring and the Analytics section for detailed analysis.

## 🎯 Getting Started

After starting the application, navigate to `http://localhost:5000` to access the main Dashboard. The system provides two primary interfaces:

- **Dashboard** (`/`) - Real-time monitoring and system overview
- **Analytics** (`/analytics`) - Comprehensive data analysis and insights

## 📊 Dashboard (Real-time Monitoring)

The Dashboard provides an at-a-glance view of your system's health with real-time updates.

### Header Section
- **Live Status Indicator** - Shows the system is actively monitoring
- **Refresh All Button** - Manually refresh all data
- **Settings Button** - Configure monitoring parameters

### System Overview Cards
The top section displays four key metrics:

1. **Overall System Health** - Aggregated health percentage with color coding:
   - 🟢 Green (95%+): Excellent health
   - 🟡 Yellow (85-94%): Warning state
   - 🔴 Red (<85%): Critical issues

2. **Healthy Services** - Count of healthy vs. total services
3. **Critical Alerts** - Number of active critical alerts
4. **Auto-Refresh** - Toggle automatic data updates

### Main Dashboard Content

#### System Alerts Panel
- **Real-time Alerts** - Latest system alerts with severity indicators
- **Alert Details** - Service ID, message, and detection time
- **View All Button** - Navigate to detailed analytics for more alerts

#### Service Rankings Panel
- **Top Performing Services** - Services ranked by health score
- **Performance Metrics** - Health percentage and availability
- **Trend Indicators** - Up/down arrows showing performance trends

#### System Health Trend Panel
- **Mini Chart** - Quick view of recent health trends
- **Multiple Services** - Shows trends for top 3 services
- **Time Indicators** - Recent performance patterns

### Quick Actions
Located at the bottom of the dashboard:

1. **View Analytics** - Navigate to comprehensive analytics section
2. **View Alerts** - See all active alerts with count
3. **Settings** - Configure monitoring preferences

### Auto-Refresh Feature
- **Toggle Switch** - Enable/disable automatic updates
- **Interval Control** - Set refresh frequency (default: 30 seconds)
- **Manual Override** - Use refresh button anytime

## 📈 Analytics Section (Detailed Analysis)

Access the Analytics section by clicking "View Analytics" from the Dashboard or navigating to `/analytics`.

### Analytics Overview Hub
The analytics landing page provides organized access to all analysis tools:

#### Quick Stats
- **Total Services** - Number of monitored services
- **Data Points Today** - Volume of collected metrics
- **Average Response Time** - System-wide performance metric
- **System Uptime** - Overall availability percentage

#### Analytics Categories
Four main analysis areas, each with dedicated pages:

### 1. Trends Analysis (`/analytics/trends`)

**Purpose**: Historical performance patterns and health trends

**Features**:
- **Time Range Selection** - 1 hour to 30 days
- **Service Filtering** - Select specific services to analyze
- **Interactive Charts** - Zoom, pan, and explore data points
- **Export Options** - Download charts as PDF, Excel, or CSV

**Key Metrics**:
- Health score trends over time
- Response time patterns
- Availability percentages
- Performance comparisons

**How to Use**:
1. Select desired time range (1h, 24h, 7d, 30d)
2. Choose services from the filter sidebar
3. Analyze trends in the interactive chart
4. Export data using the export menu

### 2. Advanced Insights (`/analytics/insights`)

**Purpose**: Anomaly detection with predictive analytics

**Features**:
- **Anomaly Detection** - Automatically identified unusual patterns
- **Service Comparisons** - Side-by-side service analysis
- **Predictive Analytics** - Future performance forecasting
- **Correlation Analysis** - Identify related performance issues

**Key Components**:
- **Anomaly Timeline** - When and where anomalies occurred
- **Severity Classification** - Critical, warning, and info levels
- **Impact Assessment** - How anomalies affect overall health
- **Prediction Confidence** - Reliability of forecasts

**How to Use**:
1. Review detected anomalies in the timeline
2. Click on anomalies for detailed analysis
3. Compare services to identify patterns
4. Review predictions for proactive planning

### 3. SLA Compliance (`/analytics/sla`)

**Purpose**: Service level agreement tracking and reporting

**Features**:
- **SLA Metrics Dashboard** - Current compliance status
- **Breach Notifications** - When SLAs are violated
- **Historical Compliance** - Trend analysis of SLA performance
- **Custom SLA Definitions** - Set your own service targets

**Key Metrics**:
- **Uptime SLA** - Availability percentage targets
- **Response Time SLA** - Performance benchmarks
- **Error Rate SLA** - Acceptable failure thresholds
- **Compliance Percentage** - Overall SLA adherence

**How to Use**:
1. Set SLA targets in the configuration panel
2. Monitor real-time compliance status
3. Review historical compliance trends
4. Investigate SLA breaches for root causes

### 4. Service Deep-Dive (`/analytics/services`)

**Purpose**: Individual service performance analysis

**Features**:
- **Service Selection** - Choose specific service to analyze
- **Comprehensive Metrics** - All available data for the service
- **Performance Breakdown** - Detailed metric categories
- **Historical Analysis** - Long-term service behavior

**Analysis Views**:
- **Health Metrics** - Current and historical health scores
- **Performance Data** - Response times, throughput, errors
- **Availability Stats** - Uptime percentages and patterns
- **Error Analysis** - Types and frequencies of issues

**How to Use**:
1. Select service from the dropdown menu
2. Choose analysis time range
3. Review comprehensive metrics dashboard
4. Export detailed reports for documentation

## 🔧 Advanced Features

### Service Filtering
Available in most analytics pages:
- **Select All/None** - Quickly manage service selection
- **Search Services** - Find services by name
- **Service Status** - See current health status
- **Bulk Operations** - Apply actions to multiple services

### Time Range Controls
Consistent across all analytics pages:
- **Quick Ranges** - 1h, 24h, 7d, 30d buttons
- **Custom Range** - Pick specific start/end dates
- **Real-time Mode** - Live updating for current data
- **Historical Mode** - Static analysis of past data

### Export Capabilities
Most analytics views support data export:
- **PDF Export** - Professional reports with charts
- **Excel Export** - Raw data for further analysis
- **CSV Export** - Data for external tools
- **Image Export** - Charts for presentations

### Navigation Features

#### Breadcrumb Navigation
- Shows your current location in the analytics hierarchy
- Click any breadcrumb to navigate back
- Provides context for nested pages

#### Menu Integration
- **Top Navigation** - Quick access to main sections
- **Analytics Dropdown** - All analytics pages listed
- **Dashboard Link** - Return to monitoring view

## 💡 Tips for Effective Use

### Dashboard Best Practices
1. **Keep Auto-Refresh Enabled** - Stay current with real-time data
2. **Monitor Alert Panel** - Check for new issues regularly
3. **Use Quick Actions** - Fast navigation to detailed analysis
4. **Set Appropriate Refresh Interval** - Balance freshness with performance

### Analytics Best Practices
1. **Start with Overview** - Use analytics hub to understand data landscape
2. **Use Appropriate Time Ranges** - Match range to analysis needs
3. **Filter Strategically** - Focus on relevant services
4. **Export Key Findings** - Document important insights
5. **Correlate Across Views** - Use multiple analytics pages together

### Performance Optimization
1. **Limit Service Selection** - Don't select all services unnecessarily
2. **Choose Reasonable Time Ranges** - Very long ranges may be slow
3. **Use Filtering** - Reduce data volume for better performance
4. **Cache Results** - Analytics data is cached for faster subsequent loads

### Troubleshooting Common Issues

#### No Data Displayed
- Check if services are properly configured
- Verify time range includes data collection period
- Ensure service filters aren't too restrictive

#### Slow Performance
- Reduce number of selected services
- Choose shorter time ranges
- Check network connectivity
- Consider system resource usage

#### Missing Features
- Verify you're using the correct analytics page
- Check if feature requires specific service configuration
- Ensure proper permissions for data access

## 🚀 Getting the Most Value

### Regular Monitoring Workflow
1. **Start with Dashboard** - Quick health check
2. **Investigate Alerts** - Address immediate issues
3. **Review Trends** - Understand performance patterns
4. **Analyze Anomalies** - Identify potential problems
5. **Check SLA Compliance** - Ensure service targets met
6. **Deep-dive Problem Services** - Detailed analysis of issues

### Proactive Monitoring
- Set up appropriate SLA targets
- Regular review of trend analysis
- Monitor anomaly detection for early warnings
- Use predictions for capacity planning

### Reporting and Documentation
- Export key metrics regularly
- Document anomaly investigations
- Share SLA compliance reports
- Create performance baselines

The Health Monitor Orleans system provides comprehensive monitoring capabilities with an intuitive interface that scales from quick dashboard checks to detailed analytical investigations.