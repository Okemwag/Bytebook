# ByteBook Platform

A modern digital book platform built with .NET 9, Entity Framework Core, and PostgreSQL, following Clean Architecture principles.

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [Make](https://www.gnu.org/software/make/) (optional, for convenience commands)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd bytebook-platform
   ```

2. **Quick start with Docker**
   ```bash
   make quick-start
   ```
   
   Or manually:
   ```bash
   docker-compose up -d
   dotnet ef database update --project ByteBook.Infrastructure --startup-project ByteBook.Api
   ```

3. **Access the application**
   - API: http://localhost:8080
   - PgAdmin: http://localhost:8082 (admin@bytebook.com / admin123)
   - Swagger UI: http://localhost:8080/swagger

## 🏗️ Architecture

The project follows Clean Architecture principles with the following layers:

```
├── ByteBook.Api/              # Web API layer (Controllers, Middleware)
├── ByteBook.Application/      # Application layer (Services, DTOs, Interfaces)
├── ByteBook.Domain/          # Domain layer (Entities, Value Objects, Events)
├── ByteBook.Infrastructure/  # Infrastructure layer (Data Access, External Services)
├── ByteBook.Shared/         # Shared utilities and common code
└── tests/                   # Unit and integration tests
```

### Key Features

- **Clean Architecture**: Separation of concerns with dependency inversion
- **Domain-Driven Design**: Rich domain models with business logic
- **CQRS Pattern**: Command Query Responsibility Segregation
- **Domain Events**: Decoupled event handling
- **Repository Pattern**: Data access abstraction
- **Value Objects**: Immutable objects for domain concepts

## 🛠️ Development

### Available Commands

```bash
# Development
make build          # Build the solution
make test           # Run all tests
make run            # Run the API locally
make run-watch      # Run with hot reload

# Database
make migrate        # Run database migrations
make migrate-add NAME=MigrationName  # Add new migration
make seed           # Seed database with sample data

# Docker
make docker-up      # Start all services
make docker-down    # Stop all services
make docker-logs    # View logs

# Environment
make dev-setup      # Set up development environment
make dev-reset      # Reset development environment
```

### Project Structure

#### Domain Layer (`ByteBook.Domain`)
- **Entities**: Core business entities (User, Book, Payment, Reading, Referral)
- **Value Objects**: Immutable objects (Email, Money, UserProfile)
- **Domain Events**: Business events for decoupled communication
- **Repositories**: Data access interfaces

#### Application Layer (`ByteBook.Application`)
- **Services**: Application services implementing business use cases
- **DTOs**: Data Transfer Objects for API communication
- **Interfaces**: Contracts for external dependencies

#### Infrastructure Layer (`ByteBook.Infrastructure`)
- **Persistence**: Entity Framework configurations and repositories
- **Events**: Domain event dispatcher implementation
- **External Services**: Third-party integrations

#### API Layer (`ByteBook.Api`)
- **Controllers**: HTTP endpoints
- **Middleware**: Cross-cutting concerns
- **Configuration**: Dependency injection and app setup

## 🐳 Docker Deployment

### Local Development

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Production Build

```bash
# Build production image
docker build -t bytebook/api:latest .

# Or use the script
./scripts/build-and-push.sh v1.0.0
```

## ☸️ Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (local or cloud)
- kubectl configured
- NGINX Ingress Controller
- cert-manager (for TLS certificates)

### Deployment Steps

1. **Create secrets**
   ```bash
   cp k8s/secret.yaml.template k8s/secret.yaml
   # Edit k8s/secret.yaml with your values
   kubectl apply -f k8s/secret.yaml
   ```

2. **Deploy the application**
   ```bash
   ./scripts/deploy.sh v1.0.0
   ```

3. **Verify deployment**
   ```bash
   kubectl get pods -n bytebook
   kubectl get services -n bytebook
   ```

### Kubernetes Resources

- **Namespace**: Isolated environment for ByteBook resources
- **ConfigMap**: Non-sensitive configuration
- **Secret**: Sensitive configuration (database credentials, JWT keys)
- **Deployments**: Application workloads (API, PostgreSQL, Redis)
- **Services**: Internal networking
- **Ingress**: External access with TLS termination
- **HPA**: Horizontal Pod Autoscaler for API scaling

## 🗄️ Database

### Entity Relationship Overview

- **Users**: Platform users (readers, authors, admins)
- **Books**: Digital books with metadata and pricing
- **Payments**: Transaction records for book purchases
- **Readings**: Reading sessions and progress tracking
- **Referrals**: User referral system with commission tracking

### Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project ByteBook.Infrastructure --startup-project ByteBook.Api

# Update database
dotnet ef database update --project ByteBook.Infrastructure --startup-project ByteBook.Api

# Remove last migration
dotnet ef migrations remove --project ByteBook.Infrastructure --startup-project ByteBook.Api
```

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/ByteBook.UnitTests
```

### Test Structure

- **Unit Tests**: Domain logic and repository tests
- **Integration Tests**: API endpoint tests
- **Performance Tests**: Load and stress testing

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Development |
| `ConnectionStrings__DefaultConnection` | Database connection | See appsettings.json |
| `Redis__ConnectionString` | Redis connection | localhost:6379 |
| `JWT__SecretKey` | JWT signing key | Required in production |

### Configuration Files

- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides
- `appsettings.Production.json`: Production overrides

## 📊 Monitoring and Observability

### Health Checks

- `/health`: Basic health check
- `/health/ready`: Readiness probe for Kubernetes

### Logging

- Structured logging with Serilog
- Log levels configurable per namespace
- Request/response logging middleware

## 🚀 Deployment Strategies

### Development
- Docker Compose for local development
- Hot reload with `dotnet watch`
- In-memory database for testing

### Staging
- Kubernetes deployment
- Separate database instance
- Feature flags for testing

### Production
- Multi-replica deployment
- Horizontal Pod Autoscaler
- Database connection pooling
- Redis caching
- TLS termination at ingress

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

### Code Style

- Follow C# coding conventions
- Use meaningful names for classes and methods
- Write unit tests for business logic
- Document public APIs

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Troubleshooting

### Common Issues

1. **Database connection errors**
   - Ensure PostgreSQL is running
   - Check connection string configuration
   - Verify database exists

2. **Migration errors**
   - Ensure EF Core tools are installed: `dotnet tool install --global dotnet-ef`
   - Check that startup project is specified correctly

3. **Docker issues**
   - Ensure Docker daemon is running
   - Check port conflicts (5432, 6379, 8080)
   - Verify Docker Compose version compatibility

### Getting Help

- Check the [Issues](../../issues) page for known problems
- Review logs: `make docker-logs` or `kubectl logs -n bytebook`
- Verify configuration: `kubectl get configmap bytebook-config -n bytebook -o yaml`

## � Useful iLinks

- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Docker Documentation](https://docs.docker.com/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)