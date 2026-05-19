#!/usr/bin/env bash
# SonarQube analysis for .NET (begin -> restore/build/test -> end).
# Requires SONAR_HOST_URL and SONAR_AUTH_TOKEN or SONAR_TOKEN (set by Jenkins withSonarQubeEnv).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-jobtracker-backend}"
SONAR_PROJECT_NAME="${SONAR_PROJECT_NAME:-JobTracker Backend}"
SONAR_PROJECT_VERSION="${SONAR_PROJECT_VERSION:-${BUILD_NUMBER:-local}}"
SONAR_CONFIGURATION="${SONAR_CONFIGURATION:-Release}"
SONAR_SETTINGS="${SONAR_SETTINGS:-${BACKEND_DIR}/SonarQube.Analysis.xml}"
SONAR_SCANNER_VERSION="${SONAR_SCANNER_VERSION:-11.2.0}"
RUN_TESTS="${SONAR_RUN_TESTS:-true}"

SONAR_TOKEN="${SONAR_AUTH_TOKEN:-${SONAR_TOKEN:-}}"
if [[ -z "${SONAR_TOKEN}" ]]; then
  echo "SONAR_AUTH_TOKEN or SONAR_TOKEN is required." >&2
  exit 1
fi
if [[ -z "${SONAR_HOST_URL:-}" ]]; then
  echo "SONAR_HOST_URL is required." >&2
  exit 1
fi

SONAR_HOST_URL="${SONAR_HOST_URL%/}"
# On the Jenkins host, start SonarQube if Docker is available and it is down.
if [[ "${SONAR_ENSURE_RUNNING:-true}" == "true" ]] && command -v docker >/dev/null \
  && [[ -f "${BACKEND_DIR}/docker-compose.sonar.yml" ]]; then
  "${SCRIPT_DIR}/ensure-sonar-server.sh" || true
fi

status_url="${SONAR_HOST_URL}/api/system/status"
if ! curl -sf "${status_url}" | grep -q '"status":"UP"'; then
  echo "Cannot reach SonarQube at ${SONAR_HOST_URL} (expected GET ${status_url} -> status UP)." >&2
  echo "On the server: docker compose -f docker-compose.sonar.yml ps && curl -sf http://127.0.0.1:9000/api/system/status" >&2
  echo "If Jenkins runs on the same host, try SONAR_HOST_URL=http://127.0.0.1:9000 in Jenkins SonarQube server settings." >&2
  exit 1
fi

export PATH="${PATH}:${HOME}/.dotnet/tools"

if ! dotnet tool list -g | grep -q '^dotnet-sonarscanner'; then
  echo "Installing dotnet-sonarscanner ${SONAR_SCANNER_VERSION}..."
  dotnet tool install --global dotnet-sonarscanner --version "${SONAR_SCANNER_VERSION}"
fi

echo "Starting SonarQube analysis for ${SONAR_PROJECT_KEY}..."
if [[ ! -f "${SONAR_SETTINGS}" ]]; then
  echo "Missing SonarQube settings file: ${SONAR_SETTINGS}" >&2
  exit 1
fi

dotnet sonarscanner begin \
  /k:"${SONAR_PROJECT_KEY}" \
  /n:"${SONAR_PROJECT_NAME}" \
  /v:"${SONAR_PROJECT_VERSION}" \
  /d:sonar.host.url="${SONAR_HOST_URL}" \
  /d:sonar.token="${SONAR_TOKEN}" \
  /d:sonar.sourceEncoding=UTF-8 \
  /s:"${SONAR_SETTINGS}"

dotnet restore JobTracker.sln
dotnet build JobTracker.sln --configuration "${SONAR_CONFIGURATION}" --no-restore

if [[ "${RUN_TESTS}" == "true" ]]; then
  dotnet test JobTracker.Tests/JobTracker.Tests.csproj \
    --configuration "${SONAR_CONFIGURATION}" \
    --no-build \
    --settings JobTracker.Tests/test.runsettings \
    --results-directory JobTracker.Tests/TestResults \
    --logger "junit;LogFileName=test_results.xml" \
    --collect:"XPlat Code Coverage"
else
  echo "Skipping tests (SONAR_RUN_TESTS=false)."
fi

dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"

# Jenkins SonarQube plugin looks for report-task.txt at the job workspace root.
TASK_REPORT="${BACKEND_DIR}/.sonarqube/out/.sonar/report-task.txt"
if [[ -f "${TASK_REPORT}" && -n "${WORKSPACE:-}" ]]; then
  dest="${WORKSPACE}/.sonarqube/out/.sonar"
  mkdir -p "${dest}"
  cp "${TASK_REPORT}" "${dest}/report-task.txt"
  echo "Published report-task.txt to Jenkins workspace (${dest})."
fi

echo "SonarQube analysis finished. View results at ${SONAR_HOST_URL}/dashboard?id=${SONAR_PROJECT_KEY}"
