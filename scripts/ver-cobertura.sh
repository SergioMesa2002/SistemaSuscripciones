#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
REPORT_DIR="$REPOSITORY_DIR/artifacts/coverage/html"
PORT="${1:-8080}"

if [[ ! "$PORT" =~ ^[0-9]+$ ]] || ((PORT < 1 || PORT > 65535)); then
  echo "El puerto debe ser un número entre 1 y 65535." >&2
  exit 1
fi

if [[ ! -f "$REPORT_DIR/index.html" ]]; then
  echo "No existe un informe. Generándolo antes de iniciar el servidor..."
  "$SCRIPT_DIR/generar-cobertura.sh"
fi

if command -v curl >/dev/null 2>&1 &&
  curl --silent --fail --max-time 2 "http://127.0.0.1:$PORT/index.html" |
    grep --quiet "Summary - Coverage Report"; then
  echo "El informe ya está disponible en http://localhost:$PORT"
  exit 0
fi

echo "Informe disponible en http://localhost:$PORT"
echo "Presiona Ctrl+C para detener el servidor."

python3 -m http.server "$PORT" \
  --bind 127.0.0.1 \
  --directory "$REPORT_DIR"
