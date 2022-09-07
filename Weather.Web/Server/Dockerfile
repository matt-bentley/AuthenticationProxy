FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

COPY [".", "/src/"]

WORKDIR src/Weather.Web/Server
RUN dotnet restore "Weather.Web.Server.csproj"

RUN dotnet publish "Weather.Web.Server.csproj" --no-restore -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER 1000
ENTRYPOINT ["dotnet", "Weather.Web.Server.dll"]