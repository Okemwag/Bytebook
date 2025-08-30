# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["ByteBook.Api/ByteBook.Api.csproj", "ByteBook.Api/"]
COPY ["ByteBook.Application/ByteBook.Application.csproj", "ByteBook.Application/"]
COPY ["ByteBook.Domain/ByteBook.Domain.csproj", "ByteBook.Domain/"]
COPY ["ByteBook.Infrastructure/ByteBook.Infrastructure.csproj", "ByteBook.Infrastructure/"]
COPY ["ByteBook.Shared/ByteBook.Shared.csproj", "ByteBook.Shared/"]

RUN dotnet restore "ByteBook.Api/ByteBook.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/ByteBook.Api"
RUN dotnet build "ByteBook.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ByteBook.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 bytebook && \
    adduser --system --uid 1001 --gid 1001 bytebook

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R bytebook:bytebook /app
USER bytebook

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ByteBook.Api.dll"]