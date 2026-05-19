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
# --remove-orphans only affects this compose project (backend app), not SonarQube (project: sonarqube).
docker compose \
  --env-file "${ENV_FILE}" \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  up -d --remove-orphans

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

echo "Deploy complete."
