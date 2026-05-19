#!/usr/bin/env bash
# Trivy vulnerability scan (filesystem + container image) via Docker.
# Requires Docker and access to /var/run/docker.sock (Jenkins agent with docker group).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${BACKEND_DIR}"

TRIVY_IMAGE="${TRIVY_IMAGE:-aquasec/trivy:latest}"
DOCKER_IMAGE="${JOBTRACKER_IMAGE:-jobtracker-api:latest}"
SEVERITY="${TRIVY_SEVERITY:-HIGH,CRITICAL}"
REPORT_DIR="${TRIVY_REPORT_DIR:-${BACKEND_DIR}/trivy-reports}"
SCAN_FS="${TRIVY_SCAN_FS:-true}"
SCAN_IMAGE="${TRIVY_SCAN_IMAGE:-true}"
FS_SCANNERS="${TRIVY_FS_SCANNERS:-vuln,secret,misconfig}"
IMAGE_SCANNERS="${TRIVY_IMAGE_SCANNERS:-vuln}"

mkdir -p "${REPORT_DIR}"

trivy_args=(
  --config /workspace/trivy.yaml
  --ignorefile /workspace/.trivyignore
  --severity "${SEVERITY}"
)

trivy_docker() {
  docker run --rm \
    -v /var/run/docker.sock:/var/run/docker.sock \
    -v "${BACKEND_DIR}:/workspace:ro" \
    -v "${REPORT_DIR}:/reports:rw" \
    "${TRIVY_IMAGE}" \
    "$@"
}

run_fs_scan() {
  echo "=== Trivy filesystem scan (${FS_SCANNERS}) ==="
  trivy_docker fs \
    "${trivy_args[@]}" \
    --scanners "${FS_SCANNERS}" \
    --format table \
    /workspace

  trivy_docker fs \
    "${trivy_args[@]}" \
    --scanners "${FS_SCANNERS}" \
    --format json \
    --output /reports/trivy-fs-report.json \
    --exit-code 1 \
    /workspace

  trivy_docker fs \
    "${trivy_args[@]}" \
    --scanners "${FS_SCANNERS}" \
    --format sarif \
    --output /reports/trivy-fs-report.sarif \
    /workspace
}

run_image_scan() {
  if ! docker image inspect "${DOCKER_IMAGE}" >/dev/null 2>&1; then
    echo "Docker image ${DOCKER_IMAGE} not found. Build the image before running Trivy." >&2
    exit 1
  fi

  echo "=== Trivy container image scan: ${DOCKER_IMAGE} (${IMAGE_SCANNERS}) ==="
  trivy_docker image \
    "${trivy_args[@]}" \
    --scanners "${IMAGE_SCANNERS}" \
    --format table \
    "${DOCKER_IMAGE}"

  trivy_docker image \
    "${trivy_args[@]}" \
    --scanners "${IMAGE_SCANNERS}" \
    --format json \
    --output /reports/trivy-image-report.json \
    --exit-code 1 \
    "${DOCKER_IMAGE}"

  trivy_docker image \
    "${trivy_args[@]}" \
    --scanners "${IMAGE_SCANNERS}" \
    --format sarif \
    --output /reports/trivy-image-report.sarif \
    "${DOCKER_IMAGE}"
}

if [[ "${SCAN_FS}" == "true" ]]; then
  run_fs_scan
fi

if [[ "${SCAN_IMAGE}" == "true" ]]; then
  run_image_scan
fi

echo "Trivy scans finished. Reports: ${REPORT_DIR}/"
