#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

current_dir="$(pwd)"
run_dir="$current_dir/run/$MPC_NODE_NUM"

ensure_commands_exists dotnet tar ssh scp xz

project_dir="$current_dir/../../CollaborativeZkVmExperiment"
project_configuration=Release
project_platform="linux-x64"

program_dir="$run_dir/bin"

if [ -d "$program_dir" ]; then
    rm -r -- "$program_dir"
fi

echo "Compiling..."
(
    cd -- "$project_dir"
    dotnet build -c "$project_configuration" -r "$project_platform" -o "$program_dir" --self-contained
)

echo "Compressing..."
(
    cd -- "$run_dir"
    XZ_OPT=-0 tar -Jcf bin.tar.xz -- bin/
)

echo "Distributing..."
(
    cd -- "$run_dir"

    pids=()  # Array to hold process IDs of background jobs
    errors=0 # Error counter
    for ((i = 0; i < $CONTROL_NODE_NUM; i++)); do
        (
            ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
            ssh "$ssh_user_host" -- "mkdir -p ~/$PROJECT_REMOTE_DIR_NAME/$MPC_NODE_NUM/"
            scp -- bin.tar.xz "$ssh_user_host:~/$PROJECT_REMOTE_DIR_NAME/$MPC_NODE_NUM/"
            ssh "$ssh_user_host" -- "cd ~/$PROJECT_REMOTE_DIR_NAME/$MPC_NODE_NUM/ && tar -Jxf bin.tar.xz"
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
)

echo Done.