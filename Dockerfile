# Use Microsoft's official build .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-sdk/
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy csproj and restore dependencies
COPY . ./
RUN dotnet restore
# Copy everything else and build the project
RUN dotnet publish PrayerTimes.Api -c Release -o out

# Use Microsoft's official runtime .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-aspnet/
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
COPY --from=build /app/out ./
# Start the app
ENTRYPOINT ["dotnet", "PrayerTimes.Api.dll"]