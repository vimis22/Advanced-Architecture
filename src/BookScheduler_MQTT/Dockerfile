# Stage 1: Build stage
# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy only the project file(s) to leverage Docker cache for restoring dependencies
COPY *.csproj ./

# Restore NuGet packages
RUN dotnet restore

# Copy the rest of the application code
COPY . ./

# Publish the application in Release mode to the /out directory
RUN dotnet publish -c Release -o out

# Stage 2: Runtime stage
# Use a smaller runtime-only image for running the application
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Set the working directory inside the runtime container
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Set environment variable for the MQTT broker host
ENV MQTT_HOST=mosquitto

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "BookScheduler.dll"]
