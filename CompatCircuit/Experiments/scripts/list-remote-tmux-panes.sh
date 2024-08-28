#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

for ((i = 0; i < $CONTROL_NODE_NUM; i++)); do
        ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
        echo "==== Node $i ===="
        ssh "$ssh_user_host" -- 'tmux list-panes -a' || true
done