#!/usr/bin/env bash

set -euo pipefail

raiz_proyecto="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
directorio_web="$raiz_proyecto/src/SistemaSuscripciones.Web"
proyecto_api="$raiz_proyecto/src/SistemaSuscripciones.Api/SistemaSuscripciones.Api.csproj"
directorio_nvm="${NVM_DIR:-$HOME/.nvm}"
servidor_cobertura_pid=""
habilitar_cobertura=false
puerto_cobertura="${COBERTURA_PORT:-8080}"

case "${1:-}" in
  "") ;;
  --cobertura) habilitar_cobertura=true ;;
  *)
    echo "Uso: ./ejecutar-integrado.sh [--cobertura]" >&2
    exit 1
    ;;
esac

limpiar_servidor_cobertura() {
  if [[ -n "$servidor_cobertura_pid" ]] && kill -0 "$servidor_cobertura_pid" 2>/dev/null; then
    kill "$servidor_cobertura_pid" 2>/dev/null || true
    wait "$servidor_cobertura_pid" 2>/dev/null || true
  fi
}

trap limpiar_servidor_cobertura EXIT

if [[ -s "$directorio_nvm/nvm.sh" ]]; then
  export NVM_DIR="$directorio_nvm"
  source "$NVM_DIR/nvm.sh"
fi

if ! command -v node >/dev/null 2>&1 || ! command -v npm >/dev/null 2>&1; then
  echo "No se encontró Node.js. Instala o activa Node 22 antes de continuar." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "No se encontró el SDK de .NET." >&2
  exit 1
fi

cd "$directorio_web"

if command -v nvm >/dev/null 2>&1; then
  nvm use --silent
fi

version_node="$(node --version | sed 's/^v//; s/\..*$//')"
if (( version_node < 22 )); then
  echo "Se requiere Node.js 22 o posterior. Versión encontrada: $(node --version)." >&2
  exit 1
fi

if [[ ! -d node_modules ]]; then
  npm ci
fi

echo "Compilando Angular para ASP.NET Core..."
npm run build:integrated

cd "$raiz_proyecto"

if [[ "$habilitar_cobertura" == true ]]; then
  echo "Generando el informe de cobertura..."
  "$raiz_proyecto/scripts/generar-cobertura.sh"

  if command -v curl >/dev/null 2>&1 &&
    curl --silent --fail --max-time 2 "http://127.0.0.1:$puerto_cobertura/index.html" |
      grep --quiet "Summary - Coverage Report"; then
    echo "El informe de cobertura ya está disponible en http://localhost:$puerto_cobertura. Se reutilizará el servidor existente."
  else
    echo "Iniciando el informe de cobertura en http://localhost:$puerto_cobertura ..."
    "$raiz_proyecto/scripts/ver-cobertura.sh" "$puerto_cobertura" &
    servidor_cobertura_pid=$!

    sleep 1
    if ! kill -0 "$servidor_cobertura_pid" 2>/dev/null; then
      if ! wait "$servidor_cobertura_pid"; then
        echo "El puerto $puerto_cobertura está ocupado por otro servicio." >&2
        echo "La aplicación continuará sin detener ese proceso. Usa otro puerto con COBERTURA_PORT, por ejemplo:" >&2
        echo "COBERTURA_PORT=8081 ./ejecutar-integrado.sh --cobertura" >&2
      fi
      servidor_cobertura_pid=""
    fi
  fi
fi

echo "Iniciando la aplicación integrada en https://localhost:7275 ..."
dotnet run --project "$proyecto_api" --launch-profile integrated
