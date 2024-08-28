## Steps

Prepare some Debian nodes, including one controller node and several worker node.
Each node should have $4m$ vCPUs, $16m$ GB RAM, $20m$ GB disk space, where $m$ is the number of MPC instances run on a *single* node.

Edit `config.sh` file.
Specify `NODE_IPS` and `CONTROL_NODE_NUM`.

(a) For single party, specify only $1$ node in `NODE_IPS`, and therefore `CONTROL_NODE_NUM` is also $1$.
(b) For $m$ multi parties running in single node, repeating the only one node for $m$ times in `NODE_IPS` and specify `CONTROL_NODE_NUM` as $1$.
(c) For $n$ multi parties each in a node, specify $n$ nodes in `NODE_IPS` and specify `CONTROL_NODE_NUM` as $n$.

```bash
nano config.sh
```

Edit `exp-constants.sh` if you want to run only partial experiment instances

```bash
nano exp-constants.sh
```

Run experiments.

```bash
bash install-tmux-on-remote-debian.sh

bash 0-build-and-distribute-programs.sh
bash 1-exp-1-prepare.sh
bash 2-exp-1-distribute.sh
bash 4-exp-23-prepare.sh
bash 5-exp-23-distribute.sh

# run one of these depending on your configuration
bash 7a-exp-123-single.sh
bash 7b-exp-123-mpc-thread.sh
bash 7c-exp-123-mpc.sh

# monitor the process
bash monitor-remote-latest-log.sh

# download logs
bash download-remote-log.sh
bash download-remote-r1cs.sh
```