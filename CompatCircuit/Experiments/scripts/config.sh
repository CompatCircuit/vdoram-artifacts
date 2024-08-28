# Note: you can specify CONTROL_NODE_NUM=1 and repeating NODE_IPs to run "mpc-thread". Otherwise, CONTROL_NODE_NUM should equals MPC_NODE_NUM, i.e., the count of NODE_IPS.
NODE_IPS=(
    "172.16.0.100"
    "172.16.0.101"
)
CONTROL_NODE_NUM=${#NODE_IPS[@]}
MPC_NODE_NUM=${#NODE_IPS[@]}

NODE_SSH_USERNAME="root"

PROJECT_REMOTE_DIR_NAME="zkvm_exp"
PROJECT_NAME="zkvm_exp"

EXP_REPEAT_COUNT=3