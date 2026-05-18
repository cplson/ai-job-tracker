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

api_container_running() {
  [[ "$(docker inspect -f '{{.State.Running}}' "${API_CONTAINER}" 2>/dev/null || echo false)" == "true" ]]
}

curl_via_api_container() {
  docker run --rm --network "container:${API_CONTAINER}" "${CURL_IMAGE}" "$@"
}

http_status_from_api() {
  local code
  code="$(curl_via_api_container -s -o /dev/null -w '%{http_code}' "${TARGET_URL}/" 2>/dev/null || true)"
  # Trim whitespace; failed curl must not be treated as ready (avoid "000" + "000" from || echo).
  echo "${code//[[:space:]]/}"
}

echo "Starting API stack for ZAP scan..."
compose up -d postgres api

if ! api_container_running; then
  echo "API container ${API_CONTAINER} is not running." >&2
  compose logs api postgres || true
  exit 1
fi
echo "Scan target: ${TARGET_URL} (network namespace: ${API_CONTAINER})"

echo "Waiting for API..."
for i in $(seq 1 60); do
  code="$(http_status_from_api)"
  if [[ "${code}" =~ ^[0-9]{3}$ && "${code}" != "000" ]]; then
    echo "API responded with HTTP ${code}."
    break
  fi
  if [[ "${i}" -eq 60 ]]; then
    echo "API did not become ready in time (last status: ${code:-none})." >&2
    compose logs api postgres || true
    exit 1
  fi
  sleep 2
done

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
