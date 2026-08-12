# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/GastroLeinefeldeAPI/GastroLeinefeldeAPI.csproj src/GastroLeinefeldeAPI/
RUN dotnet restore src/GastroLeinefeldeAPI/GastroLeinefeldeAPI.csproj

# Copy everything else and build
COPY src/ .
WORKDIR /src/GastroLeinefeldeAPI
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "GastroLeinefeldeAPI.dll"]