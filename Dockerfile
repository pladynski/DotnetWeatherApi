FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /usr/app

COPY . /usr/app/

# Build & publish only the class library (avoid sln/extra-project ambiguity).
RUN dotnet publish WeatherApi/WeatherApi.csproj -c Release -o /usr/app/publish

# Install the Graftcode gateway (gg).
RUN apt-get update && apt-get install -y wget && wget -O /usr/app/gg.deb https://github.com/grft-dev/graftcode-gateway/releases/latest/download/gg_linux_amd64.deb && dpkg -i /usr/app/gg.deb && rm /usr/app/gg.deb && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /usr/app/publish

# Port 80 = WS/service calls (ws://host:80/ws); Port 81 = Graftcode Vision UI (http://localhost:81/GV)
EXPOSE 80
EXPOSE 81

# Local default; PaaS providers (e.g. Render) inject their own PORT and route a single public port to it.
ENV PORT=80

# - The WebSocket server (consumer endpoint, /ws) listens on $PORT so PaaS routing works (Render sets PORT).
# - GRAFT_PROJECT_KEY (optional): pass a Graftcode portal key to keep a STABLE GUID/install URL across
#   restarts & deploys; the flag is only added when the env var is non-empty.
# - Credentials WEATHER_API_URL / WEATHER_API_KEY are supplied at runtime as env vars.
CMD ["sh", "-c", "gg --modules WeatherService.dll --port ${PORT:-80} ${GRAFT_PROJECT_KEY:+--projectKey $GRAFT_PROJECT_KEY}"]
