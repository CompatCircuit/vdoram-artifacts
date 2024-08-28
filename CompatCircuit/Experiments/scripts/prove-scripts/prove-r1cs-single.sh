#!/usr/bin/env bash
set -euo pipefail

source -- ./config.sh

bash generate-local-host-file.sh

(
    cd -- "$R1CS_PATH"

    # Warning: do not contain space or special characters in the filename
    for file in $(ls -lS *.single.r1cs.json | awk '{print $9}'); do
        # Extracting the circuit name (removing path and extension)
        circuit_name=$(basename "$file" ".single.r1cs.json")

        stdout_name="${circuit_name}.single.stdout"
        stderr_name="${circuit_name}.single.stderr"
        >"${stderr_name}"
        date +%s >>"${stderr_name}"
        ("$BIN_CLIENT" --hosts hosts_1 -d PlonkCompatCircuitLocal "${file}" --party 0 2>>"${stderr_name}" | tee "${stdout_name}") || true
        date +%s >>"${stderr_name}"

        sleep 10
    done
)
