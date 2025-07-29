# Artifact Evaluation Step-By-Step Guide

This document provides a comprehensive guide for the evaluation of the accompanying artifact. It details the procedures for environment configuration, compilation of source code, and execution of all experiments.

## I. Preparation

The initial phase involves preparing the evaluation environment.

### 1. System Requirements

A machine with the following minimum specifications is required:

- Operating System: Ubuntu Server 24.04 LTS (amd64)
- CPU: 8 cores
- Memory: 16 GB
- Disk Space: 40 GB free

The procedures outlined herein have been validated on a fresh, minimal installation of Ubuntu Server 24.04.2 with SSH access enabled. This guide assumes the username is `user`.

### 2. Basic Dependency Installation

The following command updates the system and installs the required dependencies.

```bash
sudo apt update && sudo apt full-upgrade -y && sudo apt install -y nano dotnet-sdk-8.0 build-essential curl tmux tar xz-utils openssh-client git unzip dos2unix jq htop python3 nano haveged pkg-config libssl-dev

sudo systemctl enable haveged
sudo systemctl start haveged
```

Note for users in Mainland China: It is advisable to [change the apt source mirror](https://mirrors.tuna.tsinghua.edu.cn/help/ubuntu/) before executing the command above to improve download speeds.

### 3. Rust Nightly Toolchain Installation

The project requires the nightly toolchain of the Rust programming language. The following command installs rustup (the Rust toolchain manager) and configures the nightly version as the default only if there are no existing Rust installations.

```bash
curl --proto '=https' --tlsv1.3 https://sh.rustup.rs -sSf | sh -s -- --default-toolchain nightly -y
```

Note for users in Mainland China: To utilize a regional mirror for the Rust installation, execute the following commands prior to the curl command above:

```bash
echo 'export RUSTUP_UPDATE_ROOT=https://mirrors.tuna.tsinghua.edu.cn/rustup/rustup' >> ~/.bashrc
echo 'export RUSTUP_DIST_SERVER=https://mirrors.tuna.tsinghua.edu.cn/rustup' >> ~/.bashrc
source ~/.bashrc

mkdir -vp ${CARGO_HOME:-$HOME/.cargo}

cat << EOF | tee -a ${CARGO_HOME:-$HOME/.cargo}/config.toml
[source.crates-io]
replace-with = 'mirror'

[source.mirror]
registry = "sparse+https://mirrors.tuna.tsinghua.edu.cn/crates.io-index/"

[registries.mirror]
index = "sparse+https://mirrors.tuna.tsinghua.edu.cn/crates.io-index/"
EOF
```

A successful installation will produce the following output:

```
info: default toolchain set to 'nightly-x86_64-unknown-linux-gnu'

  nightly-x86_64-unknown-linux-gnu installed - rustc 1.90.0-nightly (da58c0513 2025-07-03)


Rust is installed now. Great!
```

### 4. Environment and Installation Verification

To apply the updated environment variables to the current shell session, execute the following command:

```bash
source ~/.bashrc
```

Verification of the .NET SDK and Rust compiler installations can be performed by checking their versions.

```bash
dotnet --version
```

Expected output:
```
8.0.117
```

```bash
rustc --version
```

Expected output:
```
rustc 1.90.0-nightly (da58c0513 2025-07-03)
```

The display of the specified version numbers confirms a successful installation.

### 5. Source Code Acquisition

The subsequent step is to acquire the artifact's source code. The repository should be cloned into the user's home directory. This guide assumes the destination directory is `~/vdoram-artifacts`.

```bash
cd ~
cd vdoram-artifacts
```

A verification of the CompatCircuit directory contents should be performed to ensure all components are present.

```bash
cd CompatCircuit
ls
```

Expected output:
```
CollaborativeZkVm            CollaborativeZkVmTest  CompatCircuitCore      CompatCircuitProgramming      Examples
CollaborativeZkVmExperiment  CompatCircuit.sln      CompatCircuitCoreTest  CompatCircuitProgrammingTest  Experiments
```

Upon confirming the directory structure, proceed to project compilation.

## II. Project Compilation

The compilation process is divided into two primary components: the C# solution and the Rust client.

### 1. C# Solution Compilation

The C# solution is compiled first. Navigate to the `CompatCircuit` directory. The `dotnet restore` command is executed to resolve project dependencies, followed by `dotnet build -c Release` to compile the source code in its release configuration for optimized performance.

```bash
cd ~/vdoram-artifacts/CompatCircuit
dotnet restore
```

Expected output:
```
  Determining projects to restore...
  Restored /home/user/vdoram-artifacts/CompatCircuit/CollaborativeZkVm/Anonymous.CollaborativeZkVm.csproj (in 5.44 sec).
  Restored /home/user/vdoram-artifacts/CompatCircuit/CompatCircuitProgramming/Anonymous.CompatCircuitProgramming.csproj (in 5.44 sec).
  Restored /home/user/vdoram-artifacts/CompatCircuit/CompatCircuitProgrammingTest/Anonymous.CompatCircuitProgrammingTest.csproj (in 5.44 sec).
  Restored /home/user/vdoram-artifacts/CompatCircuit/CollaborativeZkVmExperiment/Anonymous.CollaborativeZkVmExperiment.csproj (in 5.44 sec).
  Restored /home/user/vdoram-artifacts/CompatCircuit/CompatCircuitCore/Anonymous.CompatCircuitCore.csproj (in 7 ms).
  Restored /home/user/vdoram-artifacts/CompatCircuit/CompatCircuitCoreTest/Anonymous.CompatCircuitCoreTest.csproj (in 2.72 sec).
  Restored /home/user/vdoram-artifacts/CompatCircuit/CollaborativeZkVmTest/Anonymous.CollaborativeZkVmTest.csproj (in 2.72 sec).
```

```bash
dotnet build -c Release
```

Expected output:
```
...
Build succeeded.
...
    18 Warning(s)
    0 Error(s)

Time Elapsed 00:00:10.96
```

The generation of warnings during this process is an expected outcome and does not indicate a build failure. A "Build succeeded" message signifies completion.

### 2. Rust Client Compilation

Subsequently, the Rust-based MPC client is compiled.

```bash
cd ~/vdoram-artifacts/collaborative-zksnark-mod/mpc-snarks
ls
```

Expected output:
```
Cargo.toml  CompatCircuit-examples  Makefile  analysis  bench_test.zsh  collaborative-zksnark-mod.md  data  scripts  src  test.zsh
```

```bash
cargo build --release --bin client
```

Expected output:
```
...
warning: `mpc-snarks` (bin "client") generated 20 warnings (7 duplicates)
    Finished `release` profile [optimized + debuginfo] target(s) in 31.47s
warning: the following packages contain code that will be rejected by a future version of Rust: ark-poly-commit v0.2.0 (/home/user/vdoram-artifacts/collaborative-zksnark-mod/poly-commit)
note: to see what the problems were, use the option `--future-incompat-report`, or run `cargo report future-incompatibilities --id 1`
```

These warnings can be disregarded. The successful creation and basic functionality of the client executable can be verified by running it without arguments.

```bash
./target/release/client
```

Expected output:
```
error: The following required arguments were not provided:
    <computation>
    --hosts <hosts>

USAGE:
    client <computation> --hosts <hosts> --party <party>

For more information try --help
```

The resulting error message, which details missing required arguments, confirms that the executable is operational.

## III. Single-Machine Evaluation

The single-machine evaluation involves running all computational parties on the local host. This configuration requires enabling the machine to establish an SSH connection to itself.

### 1. Local SSH Access Configuration

1. **Generate an SSH key pair** if one does not already exist.

    ```bash
    ssh-keygen -t ed25519
    ```
    
    Expected output:
    ```
    Generating public/private ed25519 key pair.
    Enter file in which to save the key (/home/user/.ssh/id_ed25519): 
    Enter passphrase (empty for no passphrase): 
    Enter same passphrase again: 
    Your identification has been saved in /home/user/.ssh/id_ed25519
    ```

2. **Authorize the public key** for the current user.

    ```bash
    cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys
    ```

3. **Test the local SSH connection.**

    ```bash
    ssh user@127.0.0.1 whoami
    ```
    
    Expected output:
    ```
    user
    ```

### 2. Multi Party Preprocessing Stage

This step executes a preprocessing script to generate data required for subsequent experiments.

```bash
cd ~/vdoram-artifacts/CompatCircuit/Experiments/scripts/preprocess-scripts
dos2unix *.sh
ls
```

Expected output:
```
config.sh  data.sh  exp-constants.sh  run-preprocess.sh
```

```bash
bash run-preprocess.sh
```

Log files `log.preprocess.*.txt` will be generated in the same directory.

```bash
tail -n 1 log.preprocess.mpc-2t.exp1.repeat1.2025-07-07.18.00.01.txt
```

Expected output:
```
2025-07-07 18:00:03.577 [Information] Total time cost: 2.489972 seconds
```


### 3. Single Party Computation Stage (config-e1-n1)

#### Prepare Configuration

Navigate to the primary scripts directory and copy the relevant configuration file.

```bash
cd ~/vdoram-artifacts/CompatCircuit/Experiments/scripts
dos2unix *.sh
cp config-e1-n1.sh config.sh
```

It is important to ensure that the `NODE_SSH_USERNAME` variable within config.sh corresponds to the current username `user`.

#### Build and Distribute Programs

This script packages the compiled binaries and distributes them to the target location for the experiment.

```bash
bash 0-build-and-distribute-programs.sh
```

Expected output:
```
...
Compressing...
Distributing...
Warning: Permanently added '127.0.0.1' (ED25519) to the list of known hosts.
bin.tar.xz                                                                                                            100%   31MB 166.1MB/s   00:00    
Done.
```

**Troubleshooting Note:** If you encounter the error message `: invalid option namee-programs.sh: line 2: set: pipefail`, this typically indicates the presence of incompatible line endings. This can be resolved by executing `dos2unix *.sh`.

#### Prepare and Distribute Experiment Files

```bash
bash 1-exp-1-prepare.sh
bash 2-exp-1-distribute.sh
bash 4-exp-23-prepare.sh
bash 5-exp-23-distribute.sh
```

#### Execute All Experiments

```bash
bash 7a-exp-123-single.sh
```

Expected output:
```
You have 10 seconds to recall whether previous experiment is indeed ended. Only run one experiment at the same time. Hit Enter to confirm, Ctrl+C to terminate...
Warning: Permanently added '127.0.0.1' (ED25519) to the list of known hosts.
Please manually check whether it's ended by 'bash list-remote-latest-log.sh'. Then, download log files by 'bash download-remote-log.sh'.
```

#### Monitor for Completion

The `monitor-remote-latest-log.sh` script should be used to monitor the experiment's progress via its log output.

```bash
bash monitor-remote-latest-log.sh
```

Execution should be monitored until messages indicating the completion of the final step are observed. At this point, the monitoring script can be terminated with Ctrl+C.

Expected output:
```
Mon Jul  7 16:41:29 UTC 2025
==== Node 0 ====
-rw-rw-r-- 1 user user 29735 Jul  7 16:41 log.exp23_single.exp3_20.repeat1.2025-07-07.16.40.29.txt
2025-07-07 16:41:05.408 [Information] TV-19: 3.105265 seconds
2025-07-07 16:41:05.409 [Information] PublicOutputs: 
2025-07-07 16:41:05.409 [Information] GlobalStepCounter: 19
```

#### Collect Results and Clean Up

Following the experiment's completion, the resulting log and R1CS files must be collected. The remote directories should then be cleared in preparation for subsequent experimental runs.

```bash
bash download-remote-log.sh
bash download-remote-r1cs.sh
```

The downloaded files can be inspected.

```bash
ls run/1/log/0/
ls run/1/r1cs/0/
```

Expected output:
```
log.exp1_single.repeat1.2025-07-07.16.39.33.txt          log.exp23_single.exp2_5.repeat1.2025-07-07.16.40.03.txt
log.exp23_single.exp2_1.repeat1.2025-07-07.16.39.39.txt  log.exp23_single.exp3_16.repeat1.2025-07-07.16.40.12.txt
log.exp23_single.exp2_2.repeat1.2025-07-07.16.39.45.txt  log.exp23_single.exp3_20.repeat1.2025-07-07.16.40.29.txt
log.exp23_single.exp2_3.repeat1.2025-07-07.16.39.51.txt  log.exp23_single.exp3_4.repeat1.2025-07-07.16.40.09.txt
log.exp23_single.exp2_4.repeat1.2025-07-07.16.39.57.txt
```

```bash
exp1_single.repeat1.Addition-100000.single.r1cs.json
exp1_single.repeat1.BitDecomposition-100.single.r1cs.json
exp1_single.repeat1.Inversion-1000.single.r1cs.json
exp1_single.repeat1.Multiplication-100000.single.r1cs.json
exp1_single.repeat1.zkVM-IE.single.r1cs.json
exp23_single.exp2_1.repeat1.InstructionFetcherCircuit-Step-0.single.r1cs.json
```

The json files are only needed in the proving & verifing step. The log files contains experiment results.

```bash
tail -n 20 run/2/log/0/log.exp1_mpc_thread.repeat1.2025-07-07.16.55.30.txt
```

Expected output:
```
2025-07-07 16:55:52.063 [Information] zkVM-IE: 0.915437 seconds
2025-07-07 16:55:52.063 [Information] Random public inputs used: 2
2025-07-07 16:55:52.063 [Information] FieldBeaverTripleShare used: 745591
2025-07-07 16:55:52.063 [Information] BoolBeaverTripleShare used: 180746
2025-07-07 16:55:52.063 [Information] EdaBitsKaiShare used: 102
2025-07-07 16:55:52.063 [Information] DaBitPrioPlusShare used: 25806
2025-07-07 16:55:52.063 [Information] ==== MPC Party 1 ====
2025-07-07 16:55:52.772 [Information] Total time cost: 14.731929 seconds
2025-07-07 16:55:52.773 [Information] Step time costs:
2025-07-07 16:55:52.773 [Information] Addition-100000: 0.378713 seconds
2025-07-07 16:55:52.773 [Information] Multiplication-100000: 2.068409 seconds
2025-07-07 16:55:52.773 [Information] Inversion-1000: 5.702454 seconds
2025-07-07 16:55:52.773 [Information] BitDecomposition-100: 5.666880 seconds
2025-07-07 16:55:52.773 [Information] zkVM-IE: 0.915473 seconds
2025-07-07 16:55:52.773 [Information] Random public inputs used: 2
2025-07-07 16:55:52.773 [Information] FieldBeaverTripleShare used: 745591
2025-07-07 16:55:52.773 [Information] BoolBeaverTripleShare used: 180746
2025-07-07 16:55:52.773 [Information] EdaBitsKaiShare used: 102
2025-07-07 16:55:52.773 [Information] DaBitPrioPlusShare used: 25806
2025-07-07 16:55:52.773 [Information] Total sent (all parties): 183414482 bytes
```

Proceed with cleanup.

```bash
bash clear-remote-log.sh
bash clear-remote-r1cs.sh
```

### 4. Multi Party Computation Stage (config-e1-n2, config-e1-n4)

The procedure for executing the 2-party and 4-party computation stages is analogous to the single-party case, with slight differences in the configuration file and execution script.

#### For 2 Parties (config-e1-n2):

```bash
cd ~/vdoram-artifacts/CompatCircuit/Experiments/scripts
dos2unix *.sh # just in case
cp config-e1-n2.sh config.sh # pay attention to the file name
bash 0-build-and-distribute-programs.sh
bash 1-exp-1-prepare.sh
bash 2-exp-1-distribute.sh
bash 4-exp-23-prepare.sh
bash 5-exp-23-distribute.sh
bash 7b-exp-123-mpc-thread.sh # this line is different from the single party case
bash monitor-remote-latest-log.sh
bash download-remote-log.sh
bash download-remote-r1cs.sh
bash clear-remote-log.sh
bash clear-remote-r1cs.sh
```

Note that, the following log messages indicate the running is complete.

```
Mon Jul  7 17:03:40 UTC 2025
==== Node 0 ====
-rw-rw-r-- 1 user user 132674 Jul  7 17:02 log.exp23_mpc_thread.exp3_20.repeat1.2025-07-07.17.00.26.txt
2025-07-07 17:02:59.011 [Information] EdaBitsKaiShare used: 1076
2025-07-07 17:02:59.011 [Information] DaBitPrioPlusShare used: 272228
2025-07-07 17:02:59.012 [Information] Total sent (all parties): 1165147738 bytes
```

#### For 4 Parties (config-e1-n4):

```bash
cd ~/vdoram-artifacts/CompatCircuit/Experiments/scripts
dos2unix *.sh # just in case
cp config-e1-n4.sh config.sh # pay attention to the file name
bash 0-build-and-distribute-programs.sh
bash 1-exp-1-prepare.sh
bash 2-exp-1-distribute.sh
bash 4-exp-23-prepare.sh
bash 5-exp-23-distribute.sh
bash 7b-exp-123-mpc-thread.sh # this line is different from the single party case
bash monitor-remote-latest-log.sh
bash download-remote-log.sh
bash download-remote-r1cs.sh
bash clear-remote-log.sh
bash clear-remote-r1cs.sh
```

### 5. Proving and Verification Stage

This stage is designed to measure the performance of the ZKP setup, proof generation and verification processes.

Navigate to the proving scripts directory.

```bash
cd ~/vdoram-artifacts/CompatCircuit/Experiments/scripts/prove-scripts
dos2unix *.sh
```

#### Configure Party Count

Edit the `config.sh` file in this directory to set the `PARTY_COUNT` variable to 1, 2, or 4, corresponding to the data collected in the preceding steps.

Example for single-party (PARTY_COUNT=1):

```bash
nano config.sh
```

#### Only Preserve Selected Files

For faster evaluation, consider testing with selected files rather than the complete set. 

The proving script verifies all json files located in `"$R1CS_PATH"` (`~/vdoram-artifacts/CompatCircuit/Experiments/scripts/run/"$PARTY_COUNT"/r1cs/0/`). Note that the proving stage is primarily evaluated for Figure 7 (`Performance overhead of CompatCircuit primitives`; code name starts with `exp1_`) and Figure 9 (`Time costs of overall procedures in VDORAM`; code name starts with `exp3_`) while verification stage is primarily evaluated for Figure 9. Therefore, we can also remove other unnecessary json files. For faster evaluation, we select `exp3_4` here.

```bash
source config.sh
pushd -- "$R1CS_PATH"
find . -type f -name "*.json" ! -name "*exp1*" ! -name "*exp3_4*" -exec rm {} +
ls *.json
popd
```

Expected outout:

```
~/vdoram-artifacts/CompatCircuit/Experiments/scripts/run/1/r1cs/0 ~/vdoram-artifacts/CompatCircuit/Experiments/scripts/prove-scripts
exp23_single.exp3_4.repeat1.InstructionFetcherCircuit-Step-0.single.r1cs.json  exp23_single.exp3_4.repeat1.InstructionFetcherCircuit-Step-3.single.r1cs.json  exp23_single.exp3_4.repeat1.ZkVmCircuit-Step-1.single.r1cs.json
exp23_single.exp3_4.repeat1.InstructionFetcherCircuit-Step-1.single.r1cs.json  exp23_single.exp3_4.repeat1.MemoryTraceProverCircuit-4.single.r1cs.json        exp23_single.exp3_4.repeat1.ZkVmCircuit-Step-2.single.r1cs.json
exp23_single.exp3_4.repeat1.InstructionFetcherCircuit-Step-2.single.r1cs.json  exp23_single.exp3_4.repeat1.ZkVmCircuit-Step-0.single.r1cs.json                exp23_single.exp3_4.repeat1.ZkVmCircuit-Step-3.single.r1cs.json
~/vdoram-artifacts/CompatCircuit/Experiments/scripts/prove-scripts
```


#### Execute Proving and Verification Script

If `PARTY_COUNT` is 1:

```bash
bash prove-r1cs-single.sh # despite of the name, verification is also done here
```

Expected output:
```
Start:   Connecting
··Start:   To king 1
··End:     To king 1 ...............................................................314.718µs
··Start:   From king 1
··End:     From king 1 .............................................................313ns
End:     Connecting ................................................................333.844µs
Start:   KZG10::Setup with degree 3145727
··Start:   Generating powers of G
```

If `PARTY_COUNT` is greater than 1:

```bash
bash prove-r1cs-multiparty.sh
```

Expected output:
```
Starting processes on exp1_mpc_thread.repeat1.BitDecomposition-100
All processes started. Waiting 60 sec before checking whether they work...
[0] Line 1: Start:   Connecting
[1] Line 1: Start:   Connecting
[0] Line 2: ··Start:   To king 1
[1] Line 2: ··Start:   To king 1
[1] Line 3: ··End:     To king 1 ...............................................................22.505µs
[0] Line 3: ··End:     To king 1 ...............................................................270.427µs
[0] Line 4: ··Start:   From king 1
[1] Line 4: End:     Connecting ................................................................10.031ms
[0] Line 5: ··End:     From king 1 .............................................................46.947µs
[0] Line 6: End:     Connecting ................................................................10.500ms
[1] Line 5: Start:   KZG10::Setup with degree 3145727
[0] Line 7: Start:   KZG10::Setup with degree 3145727
[1] Line 6: ··Start:   Generating powers of G
[0] Line 8: ··Start:   Generating powers of G
Successfully start the clients. Waiting for them to exit...
```

**Troubleshooting Note:** If you get stuck at `End:     Connecting` for a long time, make sure the `haveged` service is up and running.

The log files `*.stdout` will be generated in the same directory where json files are located. A helper script `print-results.sh` is provided to print the results.

```bash
bash print-results.sh
```

Expected output:
```
/home/user/vdoram-artifacts/CompatCircuit/Experiments/scripts/prove-scripts/../run/2/r1cs/0/exp23_mpc_thread.exp3_4.repeat1.ZkVmCircuit-Step-3.party1.stdout
{
    "setup": "19.5900390000000009",
    "prove": "138.3449999999999989",
    "verify": "0.0059500000000000"
}
```

## IV. Notes
​
To accelerate the artifact evaluation process, we have trimmed down the experiments. For a complete evaluation, copy the `exp-constants-non-ae.sh` file (located in both the `scripts/preprocess-scripts` and `scripts` folders) to overwrite `exp-constants.sh` file. Note that this will require a powerful server with sufficient memory and will take significantly longer to run.
