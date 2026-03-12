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
COPY --from=build /app/out ./

EXPOSE 5000
ENTRYPOINT ["dotnet", "JobTracker.API.dll"]