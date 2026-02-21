#!/usr/bin/env bash
set -euo pipefail

# Resolve directory of this script (works with symlinks too)
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
SIM_ROOT="$(cd -- "${SCRIPT_DIR}" && pwd)"

SIM_STATE_DIR="${SIM_STATE_DIR:-${SIM_ROOT}/state}"
SIM_LOG_DIR="${SIM_LOG_DIR:-${SIM_ROOT}/logs}"

ARGS="$*"

mkdir -p "$SIM_STATE_DIR" "$SIM_LOG_DIR"
printf '%s ipmitool %s\n' "$(date -Is)" "$ARGS" >> "${SIM_LOG_DIR}/ipmitool_calls.log"

case "$ARGS" in
  "sdr type temperature"|*" sdr type temperature")
    STATE_FILE="${SIM_STATE_DIR}/ipmitool_sdr_temperature.txt"
    if [[ ! -f "$STATE_FILE" ]]; then
      echo "ipmitool simulator: missing state file: $STATE_FILE" >&2
      exit 1
    fi
    cat "$STATE_FILE"
    exit 0
    ;;

  raw\ 0x30\ 0x30\ 0x01\ 0x00|*" raw 0x30 0x30 0x01 0x00")
    exit 0
    ;;
  raw\ 0x30\ 0x30\ 0x01\ 0x01|*" raw 0x30 0x30 0x01 0x01")
    exit 0
    ;;
  raw\ 0x30\ 0x30\ 0x02\ 0xff\ 0x*|*" raw 0x30 0x30 0x02 0xff 0x"*)
    SPEED_HEX="${ARGS##* }"
    echo "$SPEED_HEX" > "${SIM_STATE_DIR}/ipmitool_last_fan_speed_hex.txt"
    exit 0
    ;;

  # Read Dell third-party PCIe card cooling behavior
  # IpmiService: raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00
  raw\ 0x30\ 0xce\ 0x01\ 0x16\ 0x05\ 0x00\ 0x00\ 0x00|*" raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00")
    # Persisted state byte (matches the "state" argument used when setting)
    # 0x00 => "ENABLED" output variant in IpmiService parser
    # 0x01 => "DISABLED" output variant in IpmiService parser
    STATE_FILE="${SIM_STATE_DIR}/ipmitool_dell_third_party_pcie_cooling_state.txt"
    if [[ ! -f "$STATE_FILE" ]]; then
      echo "0x00" > "$STATE_FILE"
    fi

    STATE="$(cat "$STATE_FILE" | tr -d '\r\n' | tr '[:lower:]' '[:upper:]')"
    case "$STATE" in
      0X00)
        # ENABLED
        echo "16 05 00 00 00 05 00 00 00 00"
        ;;
      0X01)
        # DISABLED
        echo "16 05 00 00 00 05 00 01 00 00"
        ;;
      *)
        echo "ipmitool simulator: invalid dell third-party cooling state in $STATE_FILE: $STATE" >&2
        exit 1
        ;;
    esac
    exit 0
    ;;

  # Set Dell third-party PCIe card cooling behavior
  # IpmiService: raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 {state} 0x00 0x00
  raw\ 0x30\ 0xce\ 0x00\ 0x16\ 0x05\ 0x00\ 0x00\ 0x00\ 0x05\ 0x00\ 0x*\ 0x00\ 0x00|*" raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x"*" 0x00 0x00")
    # Third from last token is the state byte (0x00 or 0x01)
    # Example: ... 0x05 0x00 0x00 0x00 0x00
    #                     ^state
    TOKENS=($ARGS)
    STATE="${TOKENS[-3]}"
    STATE_FILE="${SIM_STATE_DIR}/ipmitool_dell_third_party_pcie_cooling_state.txt"
    echo "$STATE" > "$STATE_FILE"
    exit 0
    ;;

  *)
    echo "ipmitool simulator: unsupported args: $ARGS" >&2
    exit 1
    ;;
esac