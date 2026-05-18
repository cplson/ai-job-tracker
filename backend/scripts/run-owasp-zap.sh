#!/usr/bin/env bash
# OWASP ZAP baseline scan against the API (bounded JVM for 4–8 GB hosts).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

ZAP_IMAGE="${ZAP_IMAGE:-ghcr.io/zaproxy/zaproxy:stable}"
ZAP_JAVA_OPTS="${ZAP_JAVA_OPTS:--Xmx1024m}"
API_PORT="${API_PORT:-5001}"
TARGET_URL="${ZAP_TARGET_URL:-http://127.0.0.1:${API_PORT}}"
REPORT_DIR="${ZAP_REPORT_DIR:-${BACKEND_DIR}/zap-reports}"
ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"
# Expose API on localhost (required for ZAP --network host)
COMPOSE_ARGS="${COMPOSE_ARGS:--f docker-compose.yml -f docker-compose.dev.yml}"

ensure_env_file() {
  if [[ -f "${ENV_FILE}" ]]; then
    return
  fi
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

echo "Starting API stack for ZAP scan..."
compose up -d postgres api

echo "Waiting for API at ${TARGET_URL}..."
for i in $(seq 1 60); do
  code="$(curl -s -o /dev/null -w '%{http_code}' "${TARGET_URL}/" 2>/dev/null || echo 000)"
  if [[ "${code}" != "000" ]]; then
    break
  fi
  if [[ "${i}" -eq 60 ]]; then
    echo "API did not become ready in time." >&2
    compose logs api || true
    exit 1
  fi
  sleep 2
done

echo "Running ZAP baseline (heap: ${ZAP_JAVA_OPTS})..."
docker run --rm \
  --network host \
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
