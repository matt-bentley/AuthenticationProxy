FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
COPY aspnetapp.cer /usr/local/share/ca-certificates/aspnetapp.cer
RUN update-ca-certificates
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

COPY [".", "/src/"]

WORKDIR src/Gateway
RUN dotnet restore "Gateway.csproj"

RUN dotnet publish "Gateway.csproj" --no-restore -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]