# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY . .

# Restore
RUN dotnet restore "BookScheduler.csproj"

# Build & publish
RUN dotnet publish "BookScheduler.csproj" -c Release -o /app/publish


# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BookScheduler_MQTT.dll"]
