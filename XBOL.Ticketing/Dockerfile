# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_RUNNING_IN_CONTAINER=true
WORKDIR /app/src

# Copy all project files for restore (optimize layer caching - changes to .cs files won't invalidate restore)
COPY ["XBOL.Ticketing.API/XBOL.Ticketing.API.csproj", "XBOL.Ticketing.API/"]
COPY ["XBOL.Ticketing.Core.DTO/XBOL.Ticketing.Core.DTO.csproj", "XBOL.Ticketing.Core.DTO/"]
COPY ["XBOL.Ticketing.Core.Mappers/XBOL.Ticketing.Core.Mappers.csproj", "XBOL.Ticketing.Core.Mappers/"]
COPY ["XBOL.Ticketing.Data/XBOL.Ticketing.Data.csproj", "XBOL.Ticketing.Data/"]
COPY ["XBOL.Ticketing.Core.Model/XBOL.Ticketing.Core.Model.csproj", "XBOL.Ticketing.Core.Model/"]
COPY ["XBOL.Ticketing.DynamicPricing/XBOL.Ticketing.DynamicPricing.csproj", "XBOL.Ticketing.DynamicPricing/"]

# Restore dependencies
RUN dotnet restore "XBOL.Ticketing.API/XBOL.Ticketing.API.csproj" --verbosity detailed

# Copy entire source tree
COPY . .

# Build the API project
WORKDIR "/app/src/XBOL.Ticketing.API"
RUN dotnet build "XBOL.Ticketing.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "XBOL.Ticketing.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

ARG DOCKER_IMAGE_VERSION="0.0.0.0.1"

ENV DOCKER_IMAGE_VERSION=$DOCKER_IMAGE_VERSION

# Install curl for health checks (run as root)
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Switch back to non-root user (respects $APP_UID from VS or defaults to app)
USER $APP_UID

# Copy published output from publish stage
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "XBOL.Ticketing.API.dll"]
