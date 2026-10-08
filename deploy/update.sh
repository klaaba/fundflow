#!/usr/bin/env bash
# Neuen Stand von GitHub holen, neu bauen (inklusive aller Tests) und starten.
# Aufruf auf dem Server im geklonten Repository:
#   bash deploy/update.sh
# Die Demo-Daten im Volume bleiben erhalten.
set -euo pipefail

cd "$(dirname "$0")/.."
git pull --ff-only
sudo docker compose up -d --build
sudo docker image prune -f > /dev/null
echo "FundFlow aktualisiert: $(git log --oneline -1)"
