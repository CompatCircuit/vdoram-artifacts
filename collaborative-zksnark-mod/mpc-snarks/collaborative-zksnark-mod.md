# Collaborative zk-SNARK (CompatCircuit mod)

This modification adds CompartCircuit support, taking valid JSON files as input.

## build prover client

```bash
cargo build --release --bin client
```

## manually prove a circuit

Please first copy `*.json` files here from `CompatCircuit-examples`



2 parties example:

```bash
nano data/2 # specify party IP addresses
circuit_name=demo
tmux new-session -d "tmux setw remain-on-exit on; date > party0.log; ./target/release/client --hosts data/2 -d PlonkCompatCircuitMultiParty "$circuit_name".party0.r1cs.json --party 0 | tee -a party0.log; date >> party0.log"
tmux new-session -d "tmux setw remain-on-exit on; date > party1.log; ./target/release/client --hosts data/2 -d PlonkCompatCircuitMultiParty "$circuit_name".party1.r1cs.json --party 1 | tee -a party1.log; date >> party1.log"
```

Single party example:

```bash
nano data/1 # specify party IP addresses
circuit_name=demo
tmux new-session -d "tmux setw remain-on-exit on; date > party0.log; ./target/release/client --hosts data/1 -d PlonkCompatCircuitLocal "$circuit_name".single.r1cs.json --party 0; date >> party0.log"
```