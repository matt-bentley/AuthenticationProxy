FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

COPY [".", "/src/"]

WORKDIR src/DevIdentityServer
RUN dotnet restore "DevIdentityServer.csproj"

RUN dotnet publish "DevIdentityServer.csproj" --no-restore -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DevIdentityServer.dll"]