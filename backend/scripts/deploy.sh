#!/usr/bin/env bash
# Production deploy: API containers + frontend static files to nginx.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
ROOT_DIR="$(cd "${BACKEND_DIR}/.." && pwd)"
FRONTEND_DIST="${FRONTEND_DIST:-${ROOT_DIR}/frontend/dist}"
WEB_ROOT="${WEB_ROOT:-/var/www/jobtracker}"
ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"

cd "${BACKEND_DIR}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. Copy .env.example to .env and configure secrets." >&2
  exit 1
fi

set -a
# shellcheck source=/dev/null
source "${ENV_FILE}"
set +a

echo "==> Building API image"
docker build -t jobtracker-api:latest .

echo "==> Starting production stack"
COMPOSE=(docker compose -p jobtracker --env-file "${ENV_FILE}" -f docker-compose.yml -f docker-compose.prod.yml)
# --remove-orphans only affects this compose project, not SonarQube (project: sonarqube).
if ! "${COMPOSE[@]}" up -d --remove-orphans; then
  echo "Compose up failed; removing stale app containers and retrying (named volumes are kept)..." >&2
  docker rm -f jobtracker_postgres jobtracker_api 2>/dev/null || true
  "${COMPOSE[@]}" up -d --remove-orphans
fi

echo "==> Waiting for Postgres"
for _ in $(seq 1 30); do
  if "${COMPOSE[@]}" exec -T postgres pg_isready -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

echo "==> Waiting for API (schema init + listening)"
api_ready=false
for _ in $(seq 1 60); do
  logs="$(docker logs jobtracker_api 2>&1 | tail -80 || true)"
  if echo "${logs}" | grep -qi "Failed to initialize database"; then
    echo "API failed to initialize database:" >&2
    docker logs jobtracker_api --tail 40 >&2
    exit 1
  fi
  if echo "${logs}" | grep -q "Database schema ready" \
    && echo "${logs}" | grep -q "Now listening on"; then
    api_ready=true
    break
  fi
  sleep 2
done

if [[ "${api_ready}" != "true" ]]; then
  echo "API did not become ready in time. Recent logs:" >&2
  docker logs jobtracker_api --tail 40 >&2
  exit 1
fi

if [[ -d "${FRONTEND_DIST}" ]]; then
  echo "==> Publishing frontend to ${WEB_ROOT}"
  if [[ "${EUID:-$(id -u)}" -eq 0 ]]; then
    rsync -a --delete "${FRONTEND_DIST}/" "${WEB_ROOT}/"
    chown -R www-data:www-data "${WEB_ROOT}"
    chmod -R a+rX "${WEB_ROOT}"
    nginx -t && systemctl reload nginx
  else
    sudo rsync -a --delete "${FRONTEND_DIST}/" "${WEB_ROOT}/"
    sudo chown -R www-data:www-data "${WEB_ROOT}"
    sudo chmod -R a+rX "${WEB_ROOT}"
    sudo nginx -t && sudo systemctl reload nginx
  fi
else
  echo "No frontend dist at ${FRONTEND_DIST}; skipping static publish." >&2
fi

if [[ -x "${SCRIPT_DIR}/ensure-sonar-server.sh" ]]; then
  if ! curl -sf http://127.0.0.1:9000/api/system/status 2>/dev/null | grep -q '"status":"UP"'; then
    echo "==> SonarQube is down after deploy; restarting (separate compose project: sonarqube)"
    "${SCRIPT_DIR}/ensure-sonar-server.sh"
  else
    echo "==> SonarQube still up after deploy"
  fi
fi

echo "Deploy complete."
