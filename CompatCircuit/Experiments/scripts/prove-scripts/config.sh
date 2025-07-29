PARTY_COUNT=1

MPC_SNARKS_PATH="$(pwd)/../../../../collaborative-zksnark-mod/mpc-snarks"
BIN_CLIENT="$MPC_SNARKS_PATH/target/release/client"

PROJECT_REMOTE_DIR_NAME="zkvm_exp"
PROJECT_NAME="zkvm_exp"

# Directory where *.r1cs.json files are located
R1CS_PATH="$(pwd)/../run/$PARTY_COUNT/r1cs/0"