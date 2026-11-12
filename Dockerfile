# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so `dotnet restore` stays cached
# until a package reference changes.
COPY src/Domain/Domain.csproj               src/Domain/
COPY src/Application/Application.csproj     src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj                     src/Api/
RUN dotnet restore src/Api/Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The container listens on plain HTTP; put a reverse proxy (nginx, Caddy,
# your host's load balancer) in front of it for HTTPS.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

# The aspnet image ships a non-root "app" user; $APP_UID is its id.
USER $APP_UID

ENTRYPOINT ["dotnet", "Api.dll"]
