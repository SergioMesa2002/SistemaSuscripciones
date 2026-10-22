#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
RAW_DIR="$REPOSITORY_DIR/artifacts/coverage/raw/$RUN_ID"
REPORT_DIR="$REPOSITORY_DIR/artifacts/coverage/html"

cd "$REPOSITORY_DIR"

dotnet tool restore
dotnet test tests/SistemaSuscripciones.Tests/SistemaSuscripciones.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RAW_DIR" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet tool run reportgenerator \
  "-reports:$RAW_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$REPORT_DIR" \
  "-reporttypes:Html"

echo
echo "Informe generado en: $REPORT_DIR/index.html"
