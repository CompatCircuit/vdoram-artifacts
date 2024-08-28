#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

read -t 10 -p "You have 10 seconds to think twice whether these files should be cleared. Hit Enter to confirm, Ctrl+C to terminate..."

pids=()  # Array to hold process IDs of background jobs
errors=0 # Error counter
for ((i = 0; i < $CONTROL_NODE_NUM; i++)); do
    (
        ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
        ssh "$ssh_user_host" -- "rm -v ~/$PROJECT_REMOTE_DIR_NAME/$MPC_NODE_NUM/bin/*.r1cs.json || true"
    ) &

    pid=$!       # Get the process ID of the background job
    pids+=($pid) # Store PID in array
done

# Wait for all background jobs to complete
for pid in "${pids[@]}"; do
    wait $pid || ((errors++)) # Increment errors if a job fails
done
# Check if there were any errors
if [ "$errors" -gt 0 ]; then
    echo "Error: $errors job(s) failed"
    exit 1
fi
