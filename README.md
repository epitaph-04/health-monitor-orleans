# Health Monitor Orleans

A distributed, real-time health monitoring system built with Microsoft Orleans, Blazor WebAssembly, and modern web technologies. This system provides comprehensive monitoring capabilities with elegant analytics and intelligent anomaly detection.

## 🚀 Features

### Dashboard (Real-time Overview)
- **Live System Health Monitoring** - Real-time health metrics with auto-refresh
- **Service Status Overview** - At-a-glance service health indicators
- **Critical Alerts** - Immediate notification of system issues
- **Quick Actions** - Fast access to analytics, alerts, and settings

### Analytics (Detailed Insights)
- **Trends Analysis** - Historical performance patterns and health trends
- **Advanced Insights** - Anomaly detection with predictive analytics
- **SLA Compliance** - Service level agreement tracking and reporting
- **Service Deep-Dive** - Individual service performance analysis

## 🏗️ Architecture

### Technology Stack
- **.NET 10** - Latest .NET framework with enhanced performance
- **Microsoft Orleans** - Actor-based distributed systems framework
- **Blazor WebAssembly** - Modern web UI with C# instead of JavaScript
- **MudBlazor** - Material Design components for Blazor
- **ApexCharts.Blazor** - Interactive charts and data visualization
- **Redis** - Clustering, persistence, and caching layer
- **SignalR** - Real-time communication between server and clients

### System Components
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Blazor Client │    │  Orleans Silo   │    │  Redis Cluster  │
│   (WebAssembly) │◄──►│   (Grains)      │◄──►│   (Storage)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
   User Interface        Business Logic           Data Persistence
   - Dashboard           - Health Trends          - Health History
   - Analytics           - Anomaly Detection      - Service Metrics
   - Real-time UI        - SLA Calculations       - Configuration
```

## 📁 Project Structure

```
HealthMonitor.sln
├── HealthMonitor/                    # Main ASP.NET Core host
├── HealthMonitor.Client/            # Blazor WebAssembly client
│   ├── Pages/
│   │   ├── Dashboard.razor          # Overview & monitoring
│   │   ├── AnalyticsOverview.razor  # Analytics hub
│   │   └── Analytics/               # Detailed analytics pages
│   │       ├── Trends.razor         # Performance trends
│   │       ├── Insights.razor       # Advanced insights
│   │       ├── SLA.razor           # SLA compliance
│   │       └── Services.razor       # Service analysis
│   └── Components/                  # Reusable UI components
├── HealthMonitor.Cluster/          # Orleans cluster implementation
├── HealthMonitor.Model/            # Shared data models
├── HealthMonitor.Grains.Abstraction/ # Orleans grain interfaces
└── HealthMonitor.Tests/            # Test projects
```

## 🚀 Quick Start

### Prerequisites
- **.NET 10 SDK** or later
- **Redis** server (for clustering and storage)
- **Docker** (optional, for containerized deployment)

### Running Locally

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd health-monitor-orleans
   ```

2. **Start Redis server**
   ```bash
   # Using Docker
   docker run -d -p 6379:6379 redis:latest

   # Or install Redis locally
   redis-server
   ```

3. **Build and run the application**
   ```bash
   # Restore packages
   dotnet restore

   # Run the main application
   dotnet run --project HealthMonitor
   ```

4. **Access the application**
   - **Dashboard**: http://localhost:5000
   - **Analytics**: http://localhost:5000/analytics

### Using Docker

```bash
# Build and run with Docker Compose
docker-compose up --build

# Or build individual containers
docker build -f dockerfile-server -t health-monitor .
docker run -p 5000:8080 health-monitor
```

## 🎯 Recent Updates

### Dashboard & Analytics Separation (Latest)
The UI has been elegantly refactored to provide clear separation of concerns:

- **Dashboard** now focuses purely on real-time monitoring and system overview
- **Analytics** moved to dedicated section with specialized pages for different analysis types
- **Improved Navigation** with breadcrumbs and intuitive menu structure
- **Enhanced UX** with better visual hierarchy and responsive design

## 📖 Documentation

- [Architecture Guide](docs/ARCHITECTURE.md) - Detailed technical architecture
- [User Guide](docs/USER_GUIDE.md) - How to use dashboard and analytics
- [Development Guide](docs/DEVELOPMENT.md) - Setup and contribution guidelines
- [API Documentation](docs/API.md) - Orleans grains and endpoints

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter "Category=Integration"

# Run Playwright UI tests
dotnet test HealthMonitor.PlaywrightTests
```

## 🐳 Deployment

### Docker Options
- `dockerfile-server` - Main application server
- `dockerfile-cluster` - Orleans cluster node
- `dockerfile-cluster-dashboard` - Orleans dashboard

### Configuration
- Redis connection strings in `appsettings.json`
- Health check endpoints in `healthCheckConfiguration` section
- Orleans clustering configuration

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📊 Monitoring Features

### Real-time Capabilities
- Live health score calculations
- Automatic anomaly detection
- Predictive failure analysis
- SLA breach notifications
- Performance trend analysis

### Data Visualization
- Interactive charts with ApexCharts
- Historical trend analysis
- Service comparison views
- Health score distributions
- Response time analytics

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Microsoft Orleans team for the excellent actor framework
- MudBlazor community for beautiful UI components
- ApexCharts team for powerful charting capabilities