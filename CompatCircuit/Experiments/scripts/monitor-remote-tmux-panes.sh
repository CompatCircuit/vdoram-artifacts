#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

bash list-remote-tmux-panes.sh

while true; do
    output="$(bash list-remote-tmux-panes.sh)"
    clear
    date
    echo "$output"

    total_seconds=5
    printf -- '\n'
    for ((i = total_seconds; i > 0; i--)); do
        printf -- "\r$i second(s) remaining to refresh..."
        sleep 1
    done
    printf -- '\n'
    echo "Refreshing..."
done
