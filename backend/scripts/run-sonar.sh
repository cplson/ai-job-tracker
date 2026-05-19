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
SONAR_PROPERTIES="${SONAR_PROPERTIES:-${BACKEND_DIR}/sonar-project.properties}"
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

export PATH="${PATH}:${HOME}/.dotnet/tools"

if ! dotnet tool list -g | grep -q '^dotnet-sonarscanner'; then
  echo "Installing dotnet-sonarscanner ${SONAR_SCANNER_VERSION}..."
  dotnet tool install --global dotnet-sonarscanner --version "${SONAR_SCANNER_VERSION}"
fi

echo "Starting SonarQube analysis for ${SONAR_PROJECT_KEY}..."
dotnet sonarscanner begin \
  /k:"${SONAR_PROJECT_KEY}" \
  /n:"${SONAR_PROJECT_NAME}" \
  /v:"${SONAR_PROJECT_VERSION}" \
  /d:sonar.host.url="${SONAR_HOST_URL}" \
  /d:sonar.token="${SONAR_TOKEN}" \
  /s:"${SONAR_PROPERTIES}"

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

echo "SonarQube analysis finished. View results at ${SONAR_HOST_URL}/dashboard?id=${SONAR_PROJECT_KEY}"
