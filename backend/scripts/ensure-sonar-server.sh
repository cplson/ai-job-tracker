#!/usr/bin/env bash
# Start SonarQube if stopped. Safe to run before Jenkins scans or after deploy.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

SONAR_PORT="${SONAR_PORT:-9000}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://127.0.0.1:${SONAR_PORT}}"
COMPOSE=(docker compose -f docker-compose.sonar.yml)

if curl -sf "${SONAR_HOST_URL}/api/system/status" 2>/dev/null | grep -q '"status":"UP"'; then
  echo "SonarQube already UP at ${SONAR_HOST_URL}"
  "${COMPOSE[@]}" ps
  exit 0
fi

echo "SonarQube not reachable; starting stack (project: sonarqube)..."
if [[ "$(uname -s)" == "Linux" ]] && command -v sysctl >/dev/null 2>&1; then
  sysctl -w vm.max_map_count=262144 >/dev/null 2>&1 || true
fi

"${COMPOSE[@]}" up -d

echo "Waiting for SonarQube..."
for _ in $(seq 1 60); do
  if curl -sf "${SONAR_HOST_URL}/api/system/status" 2>/dev/null | grep -q '"status":"UP"'; then
    echo "SonarQube is UP."
    "${COMPOSE[@]}" ps
    exit 0
  fi
  sleep 5
done

echo "SonarQube failed to start. Logs:" >&2
"${COMPOSE[@]}" ps -a >&2
"${COMPOSE[@]}" logs --tail 80 sonarqube >&2
exit 1
