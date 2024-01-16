# Build Container with: docker build -f .\docker\flowprotocol.dockerfile -t flowprotocol .
# Start Container with: docker run -e TemplatePath=templates -v "$(pwd)"/templates:/app/templates -p 8080:80 flowprotocol
# for multi-Platforms: https://github.com/dotnet/dotnet-docker/tree/main/samples/aspnetapp

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
WORKDIR /source
COPY *.sln .
COPY FlowProtocol/*.csproj ./FlowProtocol/
RUN dotnet restore

# copy everything else and build app
COPY FlowProtocol/. ./FlowProtocol
WORKDIR /source
RUN dotnet publish ./FlowProtocol.sln -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app ./
COPY Templates/Demo/ ./data/demo/
ENTRYPOINT ["dotnet", "FlowProtocol.dll"]