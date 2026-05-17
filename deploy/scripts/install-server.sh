#!/usr/bin/env bash
# Install production + CI dependencies on Ubuntu 24.04 (JobTracker Linode).
# Run as root: sudo bash deploy/scripts/install-server.sh

set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

if [[ "${EUID:-$(id -u)}" -ne 0 ]]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

echo "==> System packages"
apt-get update
apt-get install -y \
  ca-certificates \
  curl \
  git \
  gnupg \
  nginx \
  openjdk-17-jre-headless \
  rsync \
  ufw \
  unzip

echo "==> Docker Engine"
if ! command -v docker >/dev/null 2>&1; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo "${VERSION_CODENAME}") stable" \
    > /etc/apt/sources.list.d/docker.list
  apt-get update
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi
systemctl enable --now docker

echo "==> .NET 8 SDK (for Jenkins agent builds)"
if ! command -v dotnet >/dev/null 2>&1; then
  curl -fsSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y dotnet-sdk-8.0
fi

echo "==> Node.js 20 (optional frontend builds on server)"
if ! command -v node >/dev/null 2>&1; then
  curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  apt-get install -y nodejs
fi

echo "==> App directories"
install -d -m 0755 /opt/jobtracker
install -d -m 0755 /var/www/jobtracker
install -d -m 0755 /var/log/jobtracker
chown -R www-data:www-data /var/www/jobtracker

echo "==> 2 GB swap (safety margin during OWASP ZAP / Docker builds)"
if [[ ! -f /swapfile ]]; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  grep -q '/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

echo "==> nginx site"
INSTALL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${INSTALL_DIR}/../.." && pwd)"
NGINX_CONF="${REPO_ROOT}/deploy/nginx/jobtracker.conf"
if [[ -f "${NGINX_CONF}" ]]; then
  cp "${NGINX_CONF}" /etc/nginx/sites-available/jobtracker
  ln -sf /etc/nginx/sites-available/jobtracker /etc/nginx/sites-enabled/jobtracker
  rm -f /etc/nginx/sites-enabled/default
  nginx -t && systemctl reload nginx
fi

echo "==> Firewall (SSH, HTTP, HTTPS)"
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable

echo ""
echo "Server stack installed."
echo "Configure Jenkins agent: deploy/scripts/setup-jenkins-agent.sh"
echo "Copy .env: cp backend/.env.example backend/.env && edit secrets"
