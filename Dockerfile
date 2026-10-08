# FundFlow – Container für die öffentliche Demo (Betrieb: docs/05_Betrieb.md)

# Stufe 1: bauen und testen. Schlägt ein Test fehl, entsteht kein Image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Projektdateien zuerst, damit die Paketwiederherstellung zwischengespeichert wird.
# global.json wird bewusst nicht kopiert: Das SDK-Image bringt seine eigene 10.0-Version mit.
COPY Directory.Build.props FundFlow.slnx ./
COPY src/FundFlow.Domain/FundFlow.Domain.csproj src/FundFlow.Domain/
COPY src/FundFlow.Infrastructure/FundFlow.Infrastructure.csproj src/FundFlow.Infrastructure/
COPY src/FundFlow.Scenarios/FundFlow.Scenarios.csproj src/FundFlow.Scenarios/
COPY src/FundFlow.Web/FundFlow.Web.csproj src/FundFlow.Web/
COPY tests/FundFlow.Tests/FundFlow.Tests.csproj tests/FundFlow.Tests/
RUN dotnet restore FundFlow.slnx

COPY src/ src/
COPY tests/ tests/
# Der Rückverfolgbarkeitstest gleicht den Testkatalog mit dem Fachkonzept ab.
COPY docs/01_Fachkonzept.md docs/

RUN dotnet test FundFlow.slnx -c Release --no-restore
RUN dotnet publish src/FundFlow.Web/FundFlow.Web.csproj -c Release --no-restore -o /app

# Stufe 2: nur die Laufzeit, ohne SDK und Quellcode.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN mkdir -p /data && chown "$APP_UID" /data
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__FundFlow="Data Source=/data/fundflow.db" \
    DataProtection__KeysPath=/data/keys

# Nicht als root ausführen.
USER $APP_UID
EXPOSE 8080
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "FundFlow.Web.dll"]
