FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["PrayerTimes.Api/PrayerTimes.Api.csproj", "PrayerTimes.Api/"]
RUN dotnet restore "PrayerTimes.Api/PrayerTimes.Api.csproj"
COPY . .
WORKDIR "/src/PrayerTimes.Api"
RUN dotnet build "PrayerTimes.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "PrayerTimes.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PrayerTimes.Api.dll"]
