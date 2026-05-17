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
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"

mkdir -p "${REPORT_DIR}"

echo "Starting API stack for ZAP scan..."
docker compose -f "${COMPOSE_FILE}" up -d postgres api

echo "Waiting for API at ${TARGET_URL}..."
for i in $(seq 1 60); do
  code="$(curl -s -o /dev/null -w '%{http_code}' "${TARGET_URL}/" 2>/dev/null || echo 000)"
  if [[ "${code}" != "000" ]]; then
    break
  fi
  if [[ "${i}" -eq 60 ]]; then
    echo "API did not become ready in time." >&2
    docker compose -f "${COMPOSE_FILE}" logs api || true
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

docker compose -f "${COMPOSE_FILE}" stop api || true

# -I ignores warnings; non-zero only on FAIL-NEW in strict mode
if [[ "${ZAP_EXIT:-0}" -gt 1 ]]; then
  echo "ZAP scan reported high-severity findings. See ${REPORT_DIR}/zap-report.html" >&2
  exit 1
fi

echo "ZAP scan finished. Report: ${REPORT_DIR}/zap-report.html"
