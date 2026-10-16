#!/usr/bin/env bash

set -euo pipefail

raiz_proyecto="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
directorio_web="$raiz_proyecto/src/SistemaSuscripciones.Web"
proyecto_api="$raiz_proyecto/src/SistemaSuscripciones.Api/SistemaSuscripciones.Api.csproj"
directorio_nvm="${NVM_DIR:-$HOME/.nvm}"

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
echo "Iniciando la aplicación integrada en https://localhost:7275 ..."
exec dotnet run --project "$proyecto_api" --launch-profile integrated
