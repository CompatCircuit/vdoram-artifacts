#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

# there should be at least one parameter
[ $# -ge 1 ] || (echo Please input the command. 1>&2 && exit 1)

echo "Will execute the command: $@"
echo "Note: variables in the command will be expanded on the local machine -- this is intended. To prevent this expansion, use single quotes."
sleep 1
echo "Hint Enter key to continue..."
read -r

pids=()  # Array to hold process IDs of background jobs
errors=0 # Error counter

for ((i = 0; i < $CONTROL_NODE_NUM; i++)); do
    ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
    (ssh "$ssh_user_host" -- "$@") &

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

echo "All done."
