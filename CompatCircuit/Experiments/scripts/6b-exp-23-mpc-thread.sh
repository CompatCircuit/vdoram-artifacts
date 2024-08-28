#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

if [ ! "$MPC_NODE_NUM" -gt 1 ]; then
    echo "MPC requires at least 2 nodes"
    exit 1
fi

read -t 10 -p "You have 10 seconds to recall whether previous experiment is indeed ended. Only run one experiment at the same time. Hit Enter to confirm, Ctrl+C to terminate..."

ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[0]}"
ssh "$ssh_user_host" -- "cd ~/$PROJECT_REMOTE_DIR_NAME/$MPC_NODE_NUM/bin/ && tmux new-session -d 'tmux setw remain-on-exit on; source exp-constants.sh && for ins in "'"${EXP23_INSTANCES[@]}"; do echo ==== $ins ==== && for repeat_index in $(seq 1 '"$EXP_REPEAT_COUNT"'); do INSTANCE_NAME=exp23_mpc_thread."$ins".repeat$repeat_index ./SadPencil.CollaborativeZkVmExperiment run-mpc-zkvm-thread --program-instance-name $ins'" --unsafe-repeat-preshared; sleep 10; done; done; echo ==== All done ===='"

echo "Please manually check whether it's ended by 'bash list-remote-latest-log.sh'. Then, download log files by 'bash download-remote-log.sh'."