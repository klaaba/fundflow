#!/usr/bin/env bash
# Ersteinrichtung von FundFlow auf dem Server. Aufruf aus dem geklonten Repository:
#   sudo bash deploy/setup-server.sh
# Mehrfach ausführbar: Bereits vorhandene Einträge werden nicht doppelt angelegt.
set -euo pipefail

cd "$(dirname "$0")/.."
if [[ $EUID -ne 0 ]]; then
    echo "Bitte mit sudo ausführen: sudo bash deploy/setup-server.sh" >&2
    exit 1
fi

CADDYFILE=/etc/caddy/Caddyfile
SITE=fundflow.inspiras.de

echo "1/4 Container bauen (inklusive aller Tests) und starten …"
docker compose up -d --build

echo "2/4 Warten, bis FundFlow lokal antwortet …"
for _ in $(seq 1 30); do
    curl -fsS -o /dev/null http://127.0.0.1:8085/ && break
    sleep 2
done
curl -fsS -o /dev/null http://127.0.0.1:8085/
echo "    FundFlow antwortet auf 127.0.0.1:8085."

echo "3/4 Zugriffsprotokoll mit Löschung nach 7 Tagen einrichten …"
install -d -o caddy -g caddy -m 750 /var/log/caddy
install -m 644 deploy/logrotate-caddy-fundflow /etc/logrotate.d/caddy-fundflow
logrotate --debug /etc/logrotate.d/caddy-fundflow > /dev/null 2>&1 \
    || { echo "    logrotate-Konfiguration fehlerhaft." >&2; exit 1; }

echo "4/4 Caddy um $SITE erweitern …"
if grep -q "^$SITE" "$CADDYFILE"; then
    echo "    Block für $SITE ist bereits vorhanden – unverändert."
else
    backup="$CADDYFILE.vor-fundflow.$(date +%Y%m%d-%H%M%S)"
    cp "$CADDYFILE" "$backup"
    { echo; cat deploy/Caddyfile.fundflow; } >> "$CADDYFILE"
    if ! caddy validate --config "$CADDYFILE" --adapter caddyfile > /dev/null 2>&1; then
        cp "$backup" "$CADDYFILE"
        echo "    Neue Konfiguration ungültig – ursprüngliches Caddyfile wiederhergestellt." >&2
        exit 1
    fi
    echo "    Sicherung des bisherigen Caddyfiles: $backup"
fi
systemctl reload caddy

echo
echo "Fertig. FundFlow ist in Kürze unter https://$SITE erreichbar."
