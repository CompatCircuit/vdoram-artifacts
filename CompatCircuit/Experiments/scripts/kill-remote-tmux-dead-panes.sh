#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

# https://unix.stackexchange.com/a/596997

for ((i = 0; i < $CONTROL_NODE_NUM; i++)); do
    ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
    echo "==== Node $i ===="
    ssh "$ssh_user_host" -- 'tmux list-panes -a -F "#{pane_dead} #{pane_id}" | awk '\''/^1/ { print $2 }'\'' | xargs -l tmux kill-pane -t' || true
done

echo Done.