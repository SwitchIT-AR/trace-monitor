FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/TraceMonitor.Api/TraceMonitor.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# mtr-tiny ships mtr-packet with a cap_net_raw file capability, so no full root/suid is needed —
# the container still needs NET_RAW/NET_ADMIN in its capability set (see docker-compose.yml).
RUN apt-get update \
    && apt-get install -y --no-install-recommends mtr-tiny ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TraceMonitor.Api.dll"]
