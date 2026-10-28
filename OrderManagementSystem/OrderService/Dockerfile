# Multi-stage Dockerfile for OrderService
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["OrderService/OrderService.csproj", "OrderService/"]
RUN dotnet restore "OrderService/OrderService.csproj"

# Copy source and publish
COPY OrderService/ OrderService/
WORKDIR "/src/OrderService"
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Create persistent data directory for SQLite
RUN mkdir -p /app/data && chmod 777 /app/data

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/orders.db"
ENV InventoryService__BaseUrl="http://inventory-service:8080/"

ENTRYPOINT ["dotnet", "OrderService.dll"]
