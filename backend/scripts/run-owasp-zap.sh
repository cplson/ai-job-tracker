#!/usr/bin/env bash
# OWASP ZAP baseline scan against the API (bounded JVM for 4–8 GB hosts).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

ZAP_IMAGE="${ZAP_IMAGE:-ghcr.io/zaproxy/zaproxy:stable}"
ZAP_JAVA_OPTS="${ZAP_JAVA_OPTS:--Xmx1024m}"
API_PORT="${API_PORT:-5001}"
API_CONTAINER="${API_CONTAINER:-jobtracker_api}"
# Share the API container network namespace (Compose DNS aliases are not visible to docker run).
TARGET_URL="${ZAP_TARGET_URL:-http://127.0.0.1:5000}"
REPORT_DIR="${ZAP_REPORT_DIR:-${BACKEND_DIR}/zap-reports}"
ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"
COMPOSE_ARGS="${COMPOSE_ARGS:--f docker-compose.yml}"
WAIT_SECONDS="${WAIT_SECONDS:-180}"

ZAP_CREATED_CI_ENV=false

ensure_env_file() {
  if [[ -f "${ENV_FILE}" ]]; then
    return
  fi
  ZAP_CREATED_CI_ENV=true
  echo "No ${ENV_FILE}; writing CI defaults for ZAP scan..."
  cat > "${ENV_FILE}" <<EOF
POSTGRES_USER=jobtracker
POSTGRES_PASSWORD=ci_zap_scan_password
POSTGRES_DB=JobTrackerDb
API_PORT=${API_PORT}
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

compose() {
  docker compose --env-file "${ENV_FILE}" ${COMPOSE_ARGS} "$@"
}

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
  echo "Waiting for Postgres..."
  local max=$((WAIT_SECONDS / 2))
  for i in $(seq 1 "${max}"); do
    if compose exec -T postgres pg_isready -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" >/dev/null 2>&1; then
      echo "Postgres is ready."
      return 0
    fi
    sleep 2
  done
  echo "Postgres did not become ready in time." >&2
  compose logs postgres --tail 40 || true
  return 1
}

wait_for_api() {
  echo "Waiting for API (Kestrel)..."
  local max=$((WAIT_SECONDS / 2))
  for i in $(seq 1 "${max}"); do
    if ! api_container_running; then
      echo "API container exited before becoming ready." >&2
      api_logs | tail -50 >&2
      return 1
    fi
    if api_logs_recent | grep -q "Now listening on:"; then
      echo "API is listening."
      return 0
    fi
    if api_logs_recent | grep -qiE "Failed to initialize database|JWT Key is not configured|Unhandled exception"; then
      echo "API failed during startup." >&2
      api_logs | tail -50 >&2
      return 1
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

echo "Starting API stack for ZAP scan..."
# Fresh volume on CI avoids stale Postgres credentials from earlier failed pipeline runs.
if [[ "${ZAP_FRESH_VOLUMES:-}" == "1" || "${ZAP_CREATED_CI_ENV}" == "true" ]]; then
  compose down -v --remove-orphans 2>/dev/null || true
fi
compose up -d postgres api

wait_for_postgres
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

compose stop api || true

# -I ignores warnings; non-zero only on FAIL-NEW in strict mode
if [[ "${ZAP_EXIT:-0}" -gt 1 ]]; then
  echo "ZAP scan reported high-severity findings. See ${REPORT_DIR}/zap-report.html" >&2
  exit 1
fi

echo "ZAP scan finished. Report: ${REPORT_DIR}/zap-report.html"
