FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

COPY [".", "/src/"]

WORKDIR src/Weather.Api
RUN dotnet restore "Weather.Api.csproj"

RUN dotnet publish "Weather.Api.csproj" --no-restore -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER 1000
ENTRYPOINT ["dotnet", "Weather.Api.dll"]