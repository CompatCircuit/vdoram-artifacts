## install basic packages

```bash
apt install -y git build-essential
```

## install rust nightly

```
info: latest update on 2024-08-21, rust version 1.82.0-nightly (5aea14073 2024-08-20)
```

```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh # remember to select 'nightly' toolchain
exec bash
```

## install rust nightly (for Mainland China users)

```bash
echo 'export RUSTUP_UPDATE_ROOT=https://mirrors.tuna.tsinghua.edu.cn/rustup/rustup' >> ~/.bashrc
echo 'export RUSTUP_DIST_SERVER=https://mirrors.tuna.tsinghua.edu.cn/rustup' >> ~/.bashrc
exec bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh # remember to select 'nightly' toolchain
exec bash

mkdir -vp ${CARGO_HOME:-$HOME/.cargo}

cat << EOF | tee -a ${CARGO_HOME:-$HOME/.cargo}/config
[source.crates-io]
replace-with = 'mirror'

[source.mirror]
registry = "sparse+https://mirrors.tuna.tsinghua.edu.cn/crates.io-index/"
EOF
```

## clone and build prover client

```bash
git clone https://codeup.aliyun.com/601eb857b4f3e0ef1adb6a10/collaborative-zksnark-mod.git
cd collaborative-zksnark-mod/mpc-snarks
cargo build --release --bin client
```

## manually prove a circuit

2 parties example:

```bash
nano data/2 # for example
circuit_name=MemoryTraceProverCircuit-16
tmux new-session -d "tmux setw remain-on-exit on; date > party0.log; ./target/release/client --hosts data/2 -d PlonkCompatCircuitMultiParty "$circuit_name".party0.r1cs.json --party 0 | tee -a party0.log; date >> party0.log"
tmux new-session -d "tmux setw remain-on-exit on; date > party1.log; ./target/release/client --hosts data/2 -d PlonkCompatCircuitMultiParty "$circuit_name".party1.r1cs.json --party 1 | tee -a party1.log; date >> party1.log"
```

Single party example:

```bash
nano data/1 # for example
circuit_name=MemoryTraceProverCircuit-16
tmux new-session -d "tmux setw remain-on-exit on; date > party0.log; ./target/release/client --hosts data/1 -d PlonkCompatCircuitLocal "$circuit_name".single.r1cs.json --party 0; date >> party0.log"
```

## use scripts

```bash
nano config.sh
bash generate-local-host-file.sh

# run this script in a tmux session
bash prove-r1cs-multiparty.sh
# or this
bash prove-r1cs-single.sh
```