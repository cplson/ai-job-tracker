#!/usr/bin/env bash
# OWASP ZAP baseline scan against the API (bounded JVM for 4–8 GB hosts).
# Uses an isolated Compose project (jobtracker-zap) so "down -v" never touches production DB.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

ZAP_IMAGE="${ZAP_IMAGE:-ghcr.io/zaproxy/zaproxy:stable}"
ZAP_JAVA_OPTS="${ZAP_JAVA_OPTS:--Xmx1024m}"
ZAP_PROJECT="${ZAP_COMPOSE_PROJECT:-jobtracker-zap}"
API_CONTAINER="${API_CONTAINER:-jobtracker_zap_api}"
TARGET_URL="${ZAP_TARGET_URL:-http://127.0.0.1:5000}"
REPORT_DIR="${ZAP_REPORT_DIR:-${BACKEND_DIR}/zap-reports}"
ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"
WAIT_SECONDS="${WAIT_SECONDS:-180}"

COMPOSE=(
  docker compose
  -p "${ZAP_PROJECT}"
  --env-file "${ENV_FILE}"
  -f docker-compose.yml
  -f docker-compose.zap.yml
)

ensure_env_file() {
  if [[ -f "${ENV_FILE}" ]]; then
    return
  fi
  echo "No ${ENV_FILE}; writing CI defaults for ZAP scan..."
  cat > "${ENV_FILE}" <<EOF
POSTGRES_USER=jobtracker
POSTGRES_PASSWORD=ci_zap_scan_password
POSTGRES_DB=JobTrackerDb
API_PORT=5999
Jwt__Key=IntegrationTestJwtSigningKey_MustBe32CharsOrMore!
Jwt__Issuer=JobTrackerAPI
Jwt__Audience=JobTrackerClient
OPEN_AI_KEY=
Cors__AllowedOrigins=http://localhost:5173
EOF
}

ensure_env_file

set -a
# shellcheck source=/dev/null
source "${ENV_FILE}"
set +a

mkdir -p "${REPORT_DIR}"

api_container_running() {
  [[ "$(docker inspect -f '{{.State.Running}}' "${API_CONTAINER}" 2>/dev/null || echo false)" == "true" ]]
}

api_logs() {
  docker logs "${API_CONTAINER}" 2>&1 || true
}

api_logs_recent() {
  docker logs --tail 50 "${API_CONTAINER}" 2>&1 || true
}

wait_for_postgres() {
  echo "Waiting for Postgres (ZAP stack)..."
  local max=$((WAIT_SECONDS / 2))
  for i in $(seq 1 "${max}"); do
    if "${COMPOSE[@]}" exec -T postgres pg_isready -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" >/dev/null 2>&1 \
      && "${COMPOSE[@]}" exec -T postgres psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -c 'SELECT 1' >/dev/null 2>&1; then
      echo "Postgres is ready."
      return 0
    fi
    sleep 2
  done
  echo "Postgres did not become ready in time." >&2
  "${COMPOSE[@]}" logs postgres --tail 40 || true
  return 1
}

wait_for_api() {
  echo "Waiting for API (Kestrel)..."
  local max=$((WAIT_SECONDS / 2))
  for i in $(seq 1 "${max}"); do
    if api_logs_recent | grep -q "Now listening on:"; then
      echo "API is listening."
      return 0
    fi
    if api_logs_recent | grep -qiE "password authentication failed|JWT Key is not configured"; then
      echo "API failed during startup." >&2
      api_logs | tail -50 >&2
      return 1
    fi
    if ! api_container_running; then
      sleep 3
      if ! api_container_running; then
        echo "API container is not running." >&2
        api_logs | tail -50 >&2
        return 1
      fi
    fi
    if (( i % 5 == 0 )); then
      echo "  still waiting... (${i}/${max})"
    fi
    sleep 2
  done
  echo "API did not become ready in time." >&2
  api_logs | tail -50 >&2
  return 1
}

if docker inspect jobtracker_postgres >/dev/null 2>&1; then
  prod_volume="$(docker inspect jobtracker_postgres --format '{{range .Mounts}}{{if eq .Destination "/var/lib/postgresql/data"}}{{.Name}}{{end}}{{end}}')"
  echo "Production Postgres is running (volume: ${prod_volume:-unknown}). ZAP will use isolated project ${ZAP_PROJECT} only."
fi

echo "Starting isolated ZAP stack (project: ${ZAP_PROJECT})..."
echo "Resetting ephemeral ZAP volumes only (jobtracker_zap_pgdata)..."
"${COMPOSE[@]}" down -v --remove-orphans 2>/dev/null || true
"${COMPOSE[@]}" up -d postgres
wait_for_postgres
"${COMPOSE[@]}" up -d api
wait_for_api

if ! api_container_running; then
  echo "API container ${API_CONTAINER} is not running." >&2
  exit 1
fi
echo "Scan target: ${TARGET_URL} (network namespace: ${API_CONTAINER})"

echo "Running ZAP baseline (heap: ${ZAP_JAVA_OPTS})..."
docker run --rm \
  --network "container:${API_CONTAINER}" \
  -v "${REPORT_DIR}:/zap/wrk:rw" \
  -e "JAVA_OPTS=${ZAP_JAVA_OPTS}" \
  -e "IS_CONTAINERIZED=true" \
  "${ZAP_IMAGE}" \
  zap-baseline.py -t "${TARGET_URL}" -r zap-report.html -I || ZAP_EXIT=$?

"${COMPOSE[@]}" down 2>/dev/null || true

if [[ "${ZAP_EXIT:-0}" -gt 1 ]]; then
  echo "ZAP scan reported high-severity findings. See ${REPORT_DIR}/zap-report.html" >&2
  exit 1
fi

echo "ZAP scan finished. Report: ${REPORT_DIR}/zap-report.html"
