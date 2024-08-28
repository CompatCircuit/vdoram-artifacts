#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

rm -r -- "$run_dir/exp2files/" || true

(
    cd -- "$run_dir/bin/"

    if [ ! -f ExpConfig.json ]; then
        echo Please run 'bash 1-exp-1prepare.sh' first.
        exit 1
    fi

    ./SadPencil.CollaborativeZkVmExperiment exp-2-gen-zk-program-instance
    ./SadPencil.CollaborativeZkVmExperiment exp-3-gen-zk-program-instance
)

mkdir -p -- "$run_dir/exp23files/"
(
    cd -- "$run_dir/bin/"
    cp -- *.*.bin MpcConfig.*.json ExpConfig.json *.instance.*.json "$run_dir/exp23files/"
)
(
    cd -- "$current_dir"
    cp -- exp-constants.sh "$run_dir/exp23files/"
)

echo Done.