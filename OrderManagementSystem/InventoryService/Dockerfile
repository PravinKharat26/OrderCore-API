# Multi-stage Dockerfile for InventoryService
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["InventoryService/InventoryService.csproj", "InventoryService/"]
RUN dotnet restore "InventoryService/InventoryService.csproj"

# Copy source and publish
COPY InventoryService/ InventoryService/
WORKDIR "/src/InventoryService"
RUN dotnet publish "InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Create persistent data directory for SQLite
RUN mkdir -p /app/data && chmod 777 /app/data

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/inventory.db"

ENTRYPOINT ["dotnet", "InventoryService.dll"]
