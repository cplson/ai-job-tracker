#!/usr/bin/env bash
# Seed presentation/demo data into JobTracker PostgreSQL.
#
# Usage (from backend/):
#   ./scripts/seed-demo-data.sh           # idempotent — skips if demo user already seeded
#   ./scripts/seed-demo-data.sh --force # replace existing demo@jobtracker.app data
#
# Works when:
#   - Postgres is on localhost:5432 (dev compose or local install)
#   - Postgres runs in Docker without a host port (production compose) — uses SDK container on the compose network
#
# Login after seeding: demo@jobtracker.app / Demo123!

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

ENV_FILE="${ENV_FILE:-${BACKEND_DIR}/.env}"
FORCE=false
EXTRA_ARGS=()

for arg in "$@"; do
  case "${arg}" in
    --force) FORCE=true ;;
    *) EXTRA_ARGS+=("${arg}") ;;
  esac
done

if [[ -f "${ENV_FILE}" ]]; then
  set -a
  # shellcheck source=/dev/null
  source "${ENV_FILE}"
  set +a
fi

POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-localPgPw}"
POSTGRES_DB="${POSTGRES_DB:-JobTrackerDb}"

build_connection_string() {
  local host="$1"
  printf 'Host=%s;Port=5432;Database=%s;Username=%s;Password=%s' \
    "${host}" "${POSTGRES_DB}" "${POSTGRES_USER}" "${POSTGRES_PASSWORD}"
}

run_seed() {
  local conn="$1"
  export ConnectionStrings__DefaultConnection="${conn}"

  local dotnet_args=(run --project JobTracker.Seed/JobTracker.Seed.csproj -c Release --no-launch-profile)
  if [[ "${FORCE}" == true ]]; then
    dotnet_args+=(-- --force)
  fi
  if ((${#EXTRA_ARGS[@]} > 0)); then
    dotnet_args+=("${EXTRA_ARGS[@]}")
  fi

  echo "Seeding database..." 
  echo "  $(echo "${conn}" | sed 's/;Password=[^;]*/;Password=***/')"
  dotnet "${dotnet_args[@]}"
}

postgres_container_running() {
  docker ps --format '{{.Names}}' 2>/dev/null | grep -qx 'jobtracker_postgres'
}

postgres_port_on_host() {
  docker port jobtracker_postgres 5432/tcp 2>/dev/null | grep -q .
}

run_seed_via_docker_network() {
  local conn
  conn="$(build_connection_string postgres)"
  local network
  network="$(docker inspect jobtracker_postgres --format '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' | head -1)"

  if [[ -z "${network}" ]]; then
    echo "Could not detect Docker network for jobtracker_postgres." >&2
    exit 1
  fi

  echo "Postgres is Docker-only; running seed on network ${network}..."

  local -a docker_args=(
    run --rm
    -v "${BACKEND_DIR}:/src"
    -w /src
    --network "${network}"
    -e "ConnectionStrings__DefaultConnection=${conn}"
    mcr.microsoft.com/dotnet/sdk:8.0
    dotnet run --project JobTracker.Seed/JobTracker.Seed.csproj -c Release --no-launch-profile
  )
  if [[ "${FORCE}" == true ]]; then
    docker_args+=(-- --force)
  fi

  docker "${docker_args[@]}"
}

if [[ -n "${ConnectionStrings__DefaultConnection:-}" ]]; then
  run_seed "${ConnectionStrings__DefaultConnection}"
elif postgres_container_running && ! postgres_port_on_host; then
  run_seed_via_docker_network
else
  run_seed "$(build_connection_string localhost)"
fi
