# Multi-stage build for optimization
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["SugoBackend.csproj", "."]
RUN dotnet restore "SugoBackend.csproj"

# Copy all source files and build the project
COPY . .
RUN dotnet build "SugoBackend.csproj" -c Release -o /app/build

# Publish to runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published application
COPY --from=build /app/build .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose ports
EXPOSE 5000

# Set environment
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "SugoBackend.dll"]
