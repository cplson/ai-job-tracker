# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy entire solution into container
COPY . ./

# Restore all projects
RUN dotnet restore JobTracker.API/JobTracker.API.csproj

# Publish API project
RUN dotnet publish JobTracker.API/JobTracker.API.csproj -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN groupadd --gid 10001 appuser \
    && useradd --uid 10001 --gid appuser --shell /bin/bash --create-home appuser

COPY --from=build --chown=appuser:appuser /app/out ./

# Writable resume storage (FileHelper creates files under /app/uploads)
RUN mkdir -p /app/uploads && chown -R appuser:appuser /app/uploads

USER appuser
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "JobTracker.API.dll"]