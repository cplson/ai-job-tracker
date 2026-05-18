#!/usr/bin/env bash
# OWASP ZAP baseline scan against the API (bounded JVM for 4–8 GB hosts).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

ZAP_IMAGE="${ZAP_IMAGE:-ghcr.io/zaproxy/zaproxy:stable}"
ZAP_JAVA_OPTS="${ZAP_JAVA_OPTS:--Xmx1024m}"
API_PORT="${API_PORT:-5001}"
# Reach API via Docker Compose service name (works on Jenkins; host networking does not).
ZAP_TARGET_HOST="${ZAP_TARGET_HOST:-api}"
ZAP_TARGET_PORT="${ZAP_TARGET_PORT:-5000}"
TARGET_URL="${ZAP_TARGET_URL:-http://${ZAP_TARGET_HOST}:${ZAP_TARGET_PORT}}"
REPORT_DIR="${ZAP_REPORT_DIR:-${BACKEND_DIR}/zap-reports}"
ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"
COMPOSE_ARGS="${COMPOSE_ARGS:--f docker-compose.yml}"
CURL_IMAGE="${CURL_IMAGE:-curlimages/curl:8.5.0}"

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

api_compose_network() {
  local api_cid
  api_cid="$(compose ps -q api)"
  if [[ -z "${api_cid}" ]]; then
    echo "API container is not running." >&2
    return 1
  fi
  docker inspect -f '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' "${api_cid}"
}

curl_on_compose_network() {
  docker run --rm --network "${COMPOSE_NETWORK}" "${CURL_IMAGE}" "$@"
}

echo "Starting API stack for ZAP scan..."
compose up -d postgres api

COMPOSE_NETWORK="$(api_compose_network)" || {
  compose logs api postgres || true
  exit 1
}
echo "Using Docker network: ${COMPOSE_NETWORK}"

echo "Waiting for API at ${TARGET_URL}..."
for i in $(seq 1 60); do
  code="$(curl_on_compose_network -s -o /dev/null -w '%{http_code}' "${TARGET_URL}/" 2>/dev/null || echo 000)"
  if [[ "${code}" != "000" ]]; then
    echo "API responded with HTTP ${code}."
    break
  fi
  if [[ "${i}" -eq 60 ]]; then
    echo "API did not become ready in time." >&2
    compose logs api postgres || true
    exit 1
  fi
  sleep 2
done

echo "Running ZAP baseline (heap: ${ZAP_JAVA_OPTS})..."
docker run --rm \
  --network "${COMPOSE_NETWORK}" \
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
