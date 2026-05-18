#!/usr/bin/env bash
# Register this host as a Jenkins SSH agent. Run after install-server.sh.
#
# Required environment variables:
#   JENKINS_URL       - e.g. https://jenkins.example.edu
#   JENKINS_AGENT_NAME - e.g. jobtracker-linode
#   JENKINS_USER      - Jenkins user for agent connection (SSH username on this host)
#
# On the Jenkins controller:
#   Manage Jenkins -> Nodes -> New Node -> Permanent Agent
#   Launch method: Launch agents via SSH
#   Host: <this server's IP>
#   Credentials: SSH key for the jenkins user
#
# This script creates a dedicated jenkins user and prints the public key to add
# to the controller's credential or authorized_keys.

set -euo pipefail

AGENT_USER="${JENKINS_USER:-jenkins}"
AGENT_HOME="/home/${AGENT_USER}"

if [[ "${EUID:-$(id -u)}" -ne 0 ]]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

if ! id "$AGENT_USER" &>/dev/null; then
  useradd -m -s /bin/bash -G docker "$AGENT_USER"
fi

# install -d as root leaves .ssh owned by root; jenkins must own it before ssh-keygen
install -d -m 0700 -o "${AGENT_USER}" -g "${AGENT_USER}" "${AGENT_HOME}/.ssh"
install -d -m 0755 -o "${AGENT_USER}" -g "${AGENT_USER}" "${AGENT_HOME}/agent"

if [[ ! -f "${AGENT_HOME}/.ssh/id_ed25519" ]]; then
  sudo -u "${AGENT_USER}" ssh-keygen -t ed25519 -N "" -f "${AGENT_HOME}/.ssh/id_ed25519"
fi

chown -R "${AGENT_USER}:${AGENT_USER}" "${AGENT_HOME}/.ssh" "${AGENT_HOME}/agent"
chmod 700 "${AGENT_HOME}/.ssh"
[[ -f "${AGENT_HOME}/.ssh/id_ed25519" ]] && chmod 600 "${AGENT_HOME}/.ssh/id_ed25519"
[[ -f "${AGENT_HOME}/.ssh/id_ed25519.pub" ]] && chmod 644 "${AGENT_HOME}/.ssh/id_ed25519.pub"

echo "Jenkins agent user: ${AGENT_USER}"
echo "Add this public key to Jenkins SSH credentials (or authorized_keys for inbound SSH from controller):"
echo "---"
cat "${AGENT_HOME}/.ssh/id_ed25519.pub"
echo "---"
echo ""
echo "On Jenkins controller, create node:"
echo "  Name: ${JENKINS_AGENT_NAME:-jobtracker-linode}"
echo "  Remote root directory: /home/${AGENT_USER}/agent"
echo "  Labels: jobtracker docker dotnet"
echo "  Launch via SSH -> Host: $(hostname -I | awk '{print $1}')"
echo ""
echo "Ensure ${AGENT_USER} can run docker:"
echo "  groups ${AGENT_USER}   # should include docker"
