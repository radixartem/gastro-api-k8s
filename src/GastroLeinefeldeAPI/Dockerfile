FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/GastroLeinefeldeAPI/GastroLeinefeldeAPI.csproj \
    src/GastroLeinefeldeAPI/

RUN dotnet restore \
    src/GastroLeinefeldeAPI/GastroLeinefeldeAPI.csproj

COPY src/ src/

WORKDIR /src/src/GastroLeinefeldeAPI

RUN dotnet publish \
    GastroLeinefeldeAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

RUN useradd \
    --system \
    --create-home \
    --shell /usr/sbin/nologin \
    appuser \
    && chown -R appuser:appuser /app

COPY --from=build /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "GastroLeinefeldeAPI.dll"]