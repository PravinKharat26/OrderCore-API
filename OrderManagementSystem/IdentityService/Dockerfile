# Multi-stage Dockerfile for IdentityService
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
RUN dotnet restore "IdentityService/IdentityService.csproj"

# Copy source and publish
COPY IdentityService/ IdentityService/
WORKDIR "/src/IdentityService"
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Create persistent data directory for SQLite
RUN mkdir -p /app/data && chmod 777 /app/data

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/identity.db"

ENTRYPOINT ["dotnet", "IdentityService.dll"]
