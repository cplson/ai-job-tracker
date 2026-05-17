#!/usr/bin/env bash
# Provision a Shared 8 GB Linode (4 vCPU, 160 GB) for JobTracker + Jenkins agent.
#
# Prerequisites:
#   - linode-cli: pip install linode-cli && linode-cli configure
#   - Or set LINODE_TOKEN and use curl/API manually (see deploy/README.md)
#
# Usage:
#   export LINODE_REGION=us-ord          # pick region near Jenkins controller
#   export LINODE_LABEL=jobtracker-prod
#   ./deploy/linode/provision.sh

set -euo pipefail

REGION="${LINODE_REGION:-us-ord}"
LABEL="${LINODE_LABEL:-jobtracker-prod}"
TYPE="${LINODE_TYPE:-g6-standard-4}"   # Shared 8 GB: 4 GB RAM, 4 vCPU, 160 GB
IMAGE="${LINODE_IMAGE:-linode/ubuntu24.04}"
ROOT_PASS="${LINODE_ROOT_PASSWORD:-}"

if ! command -v linode-cli >/dev/null 2>&1; then
  echo "linode-cli is required. Install: pip install linode-cli && linode-cli configure" >&2
  exit 1
fi

echo "Creating Linode: type=${TYPE} region=${REGION} label=${LABEL}"

CREATE_ARGS=(
  --type "$TYPE"
  --region "$REGION"
  --image "$IMAGE"
  --label "$LABEL"
  --tags jobtracker,jenkins-agent
)

if [[ -n "$ROOT_PASS" ]]; then
  CREATE_ARGS+=(--root_pass "$ROOT_PASS")
else
  CREATE_ARGS+=(--authorized_keys "$(cat "${LINODE_SSH_KEY_PATH:-$HOME/.ssh/id_rsa.pub}")")
fi

linode-cli linodes create "${CREATE_ARGS[@]}"

LINODE_ID="$(linode-cli linodes list --label "$LABEL" --text --format id | tail -1)"
IP="$(linode-cli linodes list --label "$LABEL" --text --format ipv4 | tail -1)"

echo ""
echo "Linode created."
echo "  ID:  ${LINODE_ID}"
echo "  IP:  ${IP}"
echo ""
echo "Next steps:"
echo "  1. ssh root@${IP}"
echo "  2. Run: curl -fsSL https://raw.githubusercontent.com/YOUR_ORG/ai-job-tracker/main/deploy/scripts/install-server.sh | bash"
echo "     Or clone the repo and run deploy/scripts/install-server.sh locally."
