#!/usr/bin/env bash
set -euo pipefail

source -- ./config.sh

if ! [[ "$PARTY_COUNT" =~ ^[0-9]+$ ]]; then
  echo "Error: You must provide a numeric party count."
  exit 1
elif (( PARTY_COUNT > 100 )); then
  echo "Error: party count is too large."
  exit 1
fi

(
    cd -- "$R1CS_PATH"
    output_file="hosts_$PARTY_COUNT"

    # Empty the file if it already exists or create it if it doesn't
    >"$output_file"

    for ((i = 0; i < PARTY_COUNT; i++)); do
        echo "127.0.0.$((100 + i)):8000" >>"$output_file"
    done
)
