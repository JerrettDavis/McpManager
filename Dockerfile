# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["McpManager.sln", "./"]
COPY ["src/McpManager.Core/McpManager.Core.csproj", "src/McpManager.Core/"]
COPY ["src/McpManager.Application/McpManager.Application.csproj", "src/McpManager.Application/"]
COPY ["src/McpManager.Infrastructure/McpManager.Infrastructure.csproj", "src/McpManager.Infrastructure/"]
COPY ["src/McpManager.Web/McpManager.Web.csproj", "src/McpManager.Web/"]

# Restore dependencies
RUN dotnet restore "src/McpManager.Web/McpManager.Web.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/McpManager.Web"
RUN dotnet build "McpManager.Web.csproj" -c Release -o /app/build
RUN dotnet publish "McpManager.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Copy published app
COPY --from=build --chown=appuser:appuser /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/ || exit 1

# Start the application
ENTRYPOINT ["dotnet", "McpManager.Web.dll"]
