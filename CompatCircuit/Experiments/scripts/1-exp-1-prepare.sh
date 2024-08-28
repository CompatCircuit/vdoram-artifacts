#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

ensure_commands_exists jq

rm -r -- "$run_dir/exp1files/" || true

(
    cd -- "$run_dir"

    # Define the jq filter as a string
    jq_filter='{
    "party_ip_addresses": $ips
    }'

    # Use jq to construct the JSON object, passing the bash array as --argjson
    jq -n --argjson ips "$(printf '%s\n' "${NODE_IPS[@]}" | jq -Rsc 'split("\n")[:-1]')" "$jq_filter" >./ExpConfig.json
)

(
    cd -- "$run_dir/bin/"
    rm -- *.bin MpcConfig.*.json ExpConfig.json || true
    cp -- ../ExpConfig.json .
    ./SadPencil.CollaborativeZkVmExperiment exp-1-prepare-files --unsafe-repeat-preshared
    mkdir -p -- "$run_dir/exp1files/"
    cp -- *.bin MpcConfig.*.json ExpConfig.json "$run_dir/exp1files/"
)

echo Done.