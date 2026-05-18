#!/usr/bin/env bash
# Apply EF schema inside the running API container (after deploy).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"

docker compose --env-file "${ENV_FILE}" -f docker-compose.yml -f docker-compose.prod.yml up -d postgres api

echo "Waiting for API..."
for i in $(seq 1 30); do
  if docker logs jobtracker_api 2>&1 | grep -q "Database schema ready"; then
    echo "Schema initialized."
    exit 0
  fi
  if docker logs jobtracker_api 2>&1 | grep -qi "Failed to initialize database"; then
    echo "API failed to initialize database. Logs:" >&2
    docker logs jobtracker_api --tail 40 >&2
    exit 1
  fi
  sleep 2
done

echo "Timed out. Recent API logs:" >&2
docker logs jobtracker_api --tail 40 >&2
exit 1
