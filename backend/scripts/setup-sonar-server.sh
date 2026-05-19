#!/usr/bin/env bash
# Start SonarQube via Docker and print Jenkins / local scanner next steps.
# Run on the Linode (or any host) where Jenkins can reach port 9000.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

SONAR_PORT="${SONAR_PORT:-9000}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:${SONAR_PORT}}"

if [[ "$(uname -s)" == "Linux" ]] && command -v sysctl >/dev/null 2>&1; then
  current_map_count="$(sysctl -n vm.max_map_count 2>/dev/null || echo 0)"
  if [[ "${current_map_count}" -lt 262144 ]]; then
    echo "Note: SonarQube on Linux needs vm.max_map_count >= 262144 (current: ${current_map_count})."
    echo "  sudo sysctl -w vm.max_map_count=262144"
    echo "  echo 'vm.max_map_count=262144' | sudo tee -a /etc/sysctl.conf"
  fi
fi

echo "Starting SonarQube (this can take 1–2 minutes on first boot)..."
docker compose -f docker-compose.sonar.yml up -d

echo "Waiting for SonarQube at ${SONAR_HOST_URL} ..."
for _ in $(seq 1 60); do
  if curl -sf "${SONAR_HOST_URL}/api/system/status" 2>/dev/null | grep -q '"status":"UP"'; then
    echo "SonarQube is UP."
    break
  fi
  sleep 5
done

if ! curl -sf "${SONAR_HOST_URL}/api/system/status" 2>/dev/null | grep -q '"status":"UP"'; then
  echo "SonarQube did not become ready in time. Check logs: docker compose -f docker-compose.sonar.yml logs -f sonarqube" >&2
  exit 1
fi

cat <<EOF

SonarQube is running.

  URL:      ${SONAR_HOST_URL}
  Login:    admin / admin  (you will be prompted to change the password)

Next steps:

1. Open the UI and sign in.
2. My Account -> Security -> Generate Token (name e.g. jenkins-jobtracker).
3. Jenkins (Manage Jenkins -> System -> SonarQube servers):
   - Name: SonarQube   (must match SONARQUBE_ENV in Jenkinsfile)
   - Server URL: http://<linode-ip>:9000   (not localhost unless Jenkins is on the same host)
   - Server authentication token: paste the token from step 2
4. Install plugins if missing: "SonarQube Scanner" and "SonarQube" (for waitForQualityGate).
5. Run analysis locally:
     export SONAR_HOST_URL="${SONAR_HOST_URL}"
     export SONAR_TOKEN="<your-token>"
     ./scripts/run-sonar.sh

Stop server: docker compose -f docker-compose.sonar.yml down

If SonarQube stops after Deploy runs: deploy uses --remove-orphans on the app stack.
  This compose file uses project name "sonarqube" so it is not removed. Recreate if needed:
  docker compose -f docker-compose.sonar.yml up -d

If the container was OOM-killed:
  docker inspect sonarqube --format '{{.State.OOMKilled}}'
  Consider 2G swap (see docker-compose.sonar.yml comments).
EOF
