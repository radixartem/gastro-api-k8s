# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/GastroLeinefeldeAPI/GastroLeinefeldeAPI.csproj \
     src/GastroLeinefeldeAPI/

RUN dotnet restore src/GastroLeinefeldeAPI/GastroLeinefeldeAPI.csproj

COPY src/ .

WORKDIR /src/GastroLeinefeldeAPI

RUN dotnet publish \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

RUN adduser \
    --disabled-password \
    --gecos '' \
    appuser \
    && chown -R appuser:appuser /app

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GastroLeinefeldeAPI.dll"]