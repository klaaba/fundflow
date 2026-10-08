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
ACCESS_LOG=/var/log/caddy/fundflow-access.log

echo "1/4 Container bauen (inklusive aller Tests) und starten …"
docker compose up -d --build

echo "2/4 Warten, bis FundFlow lokal antwortet …"
for _ in $(seq 1 30); do
    curl -fsS -o /dev/null http://127.0.0.1:8085/ 2> /dev/null && break
    sleep 2
done
curl -fsS -o /dev/null http://127.0.0.1:8085/
echo "    FundFlow antwortet auf 127.0.0.1:8085."

echo "3/4 Zugriffsprotokoll mit Löschung nach 7 Tagen einrichten …"
install -d -o caddy -g caddy -m 750 /var/log/caddy
# Datei vorab als caddy anlegen: „caddy validate“ läuft als root und würde sie sonst als root anlegen,
# sodass der Caddy-Dienst (Benutzer caddy) sie nicht öffnen kann.
touch "$ACCESS_LOG"
chown caddy:caddy "$ACCESS_LOG"
chmod 640 "$ACCESS_LOG"
install -m 644 deploy/logrotate-caddy-fundflow /etc/logrotate.d/caddy-fundflow
logrotate --debug /etc/logrotate.d/caddy-fundflow > /dev/null 2>&1 \
    || { echo "    logrotate-Konfiguration fehlerhaft." >&2; exit 1; }

echo "4/4 Caddy um $SITE erweitern …"
backup=""
if grep -q "^$SITE" "$CADDYFILE"; then
    echo "    Block für $SITE ist bereits vorhanden – unverändert."
else
    backup="$CADDYFILE.vor-fundflow.$(date +%Y%m%d-%H%M%S)"
    cp "$CADDYFILE" "$backup"
    { echo; cat deploy/Caddyfile.fundflow; } >> "$CADDYFILE"
    echo "    Sicherung des bisherigen Caddyfiles: $backup"
fi

# Immer prüfen, bevor Caddy neu lädt – auch wenn der Block schon vorhanden war.
if ! caddy validate --config "$CADDYFILE" --adapter caddyfile > /dev/null 2>&1; then
    if [[ -n "$backup" ]]; then
        cp "$backup" "$CADDYFILE"
        echo "    Konfiguration ungültig – ursprüngliches Caddyfile wiederhergestellt." >&2
    else
        echo "    Konfiguration ungültig – bitte $CADDYFILE prüfen." >&2
    fi
    exit 1
fi
chown caddy:caddy "$ACCESS_LOG"

if ! systemctl reload caddy; then
    echo "    Caddy konnte nicht neu laden. Details: systemctl status caddy" >&2
    exit 1
fi

echo
echo "Fertig. FundFlow ist in Kürze unter https://$SITE erreichbar."
