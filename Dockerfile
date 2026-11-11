# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, on its own layer, so code changes don't re-download packages.
COPY EF.csproj ./
RUN dotnet restore EF.csproj

# EF.Tests is excluded by .dockerignore: it isn't needed to run the API.
COPY . .
RUN dotnet publish EF.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- run ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Listens on 8080 (the default for .NET 8+ images) and runs as the image's non-root user.
EXPOSE 8080
USER $APP_UID

# The connection string is not baked in: pass ConnectionStrings__DefaultConnection at run time
# (docker-compose.yml does this).
ENTRYPOINT ["dotnet", "EFApp.dll"]
