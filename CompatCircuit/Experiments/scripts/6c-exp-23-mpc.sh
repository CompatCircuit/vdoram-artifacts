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

pids=()  # Array to hold process IDs of background jobs
errors=0 # Error counter
for ((i = 0; i < $CONTROL_NODE_NUM; i++)); do
    (
        ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
        ssh "$ssh_user_host" -- "cd ~/$PROJECT_REMOTE_DIR_NAME/$MPC_NODE_NUM/bin/ && tmux new-session -d 'tmux setw remain-on-exit on; source exp-constants.sh && for ins in "'"${EXP23_INSTANCES[@]}"; do echo ==== $ins ==== && for repeat_index in $(seq 1 '"$EXP_REPEAT_COUNT"'); do INSTANCE_NAME=exp23_mpc."$ins".repeat$repeat_index ./SadPencil.CollaborativeZkVmExperiment run-mpc-zkvm --program-instance $ins'".instance.$i.json --party $i --unsafe-repeat-preshared; sleep 10; done; done; echo ==== All done ===='"
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

echo "Please manually check whether it's ended by 'bash list-remote-latest-log.sh'. Then, download log files by 'bash download-remote-log.sh'."