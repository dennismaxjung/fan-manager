#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
SIM_ROOT="$(cd -- "${SCRIPT_DIR}" && pwd)"

SIM_STATE_DIR="${SIM_STATE_DIR:-${SIM_ROOT}/state}"
SIM_LOG_DIR="${SIM_LOG_DIR:-${SIM_ROOT}/logs}"

ARGS="$*"

mkdir -p "$SIM_STATE_DIR" "$SIM_LOG_DIR"
printf '%s nvidia-smi %s\n' "$(date -Is)" "$ARGS" >> "${SIM_LOG_DIR}/nvidia_smi_calls.log"

case "$ARGS" in
  --version)
    STATE_FILE="${SIM_STATE_DIR}/nvidia_smi_version.txt"
    if [[ ! -f "$STATE_FILE" ]]; then
      echo "nvidia-smi simulator: missing state file: $STATE_FILE" >&2
      exit 1
    fi
    cat "$STATE_FILE"
    exit 0
    ;;

  "--query-gpu=uuid,name,driver_version,fan.speed --format=csv,noheader,nounits")
    STATE_FILE="${SIM_STATE_DIR}/nvidia_smi_query_gpu.csv"
    if [[ ! -f "$STATE_FILE" ]]; then
      echo "nvidia-smi simulator: missing state file: $STATE_FILE" >&2
      exit 1
    fi
    cat "$STATE_FILE"
    exit 0
    ;;

  "--query-gpu=uuid,temperature.gpu --format=csv,noheader,nounits")
    STATE_FILE="${SIM_STATE_DIR}/nvidia_smi_temps.txt"
    if [[ ! -f "$STATE_FILE" ]]; then
      echo "nvidia-smi simulator: missing state file: $STATE_FILE" >&2
      exit 1
    fi
    cat "$STATE_FILE"
    exit 0
    ;;

  *)
    echo "nvidia-smi simulator: unsupported args: $ARGS" >&2
    exit 1
    ;;
esac