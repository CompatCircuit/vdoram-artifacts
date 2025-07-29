# macOS Evaluation Guide

This document supplements the `Artifact Evaluation Step-By-Step Guide` by providing specific instructions for running the experiments on macOS. Follow these steps to set up and execute the experiments on a macOS system.

## I. Preparation

This section replaces the `I. Preparation` section in the `Artifact Evaluation Step-By-Step Guide`.

### System Requirements
We tested the following steps on a MacBook Air M4 with 16 GB RAM, running macOS Sequoia 15.5.

### 1. Install Required Software

Follow these steps to install the necessary dependencies:

#### Install .NET SDK 8.0
Download and install the .NET SDK 8.0 for macOS (ARM64):
- Visit [this link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.315-macos-arm64-installer) to download the installer.
- Follow the installation instructions provided by the installer.

#### Install Rust Nightly
Install the Rust nightly toolchain using `rustup`:
```sh
curl --proto '=https' --tlsv1.3 https://sh.rustup.rs -sSf | sh -s -- --default-toolchain nightly -y
```
After installation, run the following to apply environment variables:
```sh
source ~/.cargo/env
```

#### Install Homebrew Packages
Install the required tools using Homebrew. If Homebrew is not installed, install it first by following the instructions at [brew.sh](https://brew.sh).

Run the following commands to install the necessary packages:
```sh
brew install dos2unix
brew install tmux
brew install xz
brew install htop
brew install python
brew install bash
```

#### Configure Non-Interactive Shell
Add Homebrew commands to the non-interactive shell (used by our experiment scripts):
```sh
echo 'export PATH="/opt/homebrew/bin:$PATH"' >> ~/.zshenv
```

Close your terminal and open a new one to apply the changes.

#### Verify Bash Version
Confirm that the latest Bash version is installed:
```sh
bash --version
```

**Expected Output**:
```
GNU bash, version 5.3.3(1)-release (aarch64-apple-darwin24.4.0)
Copyright (C) 2025 Free Software Foundation, Inc.
License GPLv3+: GNU GPL version 3 or later <http://gnu.org/licenses/gpl.html>
```

#### Handle Non-English Systems
If your macOS system uses a language other than English, you may temporarily switch the output language to English for the current session:
```sh
export LC_ALL=en_US.UTF-8
```

### 2. Enable and Test SSH Server
Enable the SSH server on your macOS system:
- Follow the instructions at [this guide](https://superuser.com/questions/104929/how-do-you-run-an-ssh-server-on-macos) to enable the SSH server.

Test the SSH connection:
```sh
ssh user@127.0.0.1 which tmux
```

**Expected Output**:
```
/opt/homebrew/bin/tmux
```

### 3. Modify Experiment Script for macOS
Navigate to the scripts directory:
```sh
cd ~/vdoram-artifacts/CompatCircuit/Experiments/scripts
```

Edit the `0-build-and-distribute-programs.sh` file to use the correct platform for macOS:
1. Open the file in a text editor (e.g., `nano`):
   ```sh
   nano 0-build-and-distribute-programs.sh
   ```
2. Locate the line:
   ```bash
   project_platform="linux-x64"
   ```
3. Change it to:
   ```bash
   project_platform="osx-arm64"
   ```
4. Save and exit the editor (`Ctrl+O`, then `Ctrl+X` in nano).


## II. Running the Experiments

You are now ready to proceed with the experiment setup and execution. Follow the instructions starting from **Section II. Project Compilation** in the `Artifact Evaluation Step-By-Step Guide` to continue.

### Important Notes
- **RAM Monitoring**: Use the `htop` command to monitor RAM usage during experiments. Due to limited available memory on a 16 GB Mac, we recommend running only the single-party computation case (`config-e1-n1`) to avoid performance issues.
- **macOS Limitations**: Our CompatCircuit and VDORAM programs should fully support Windows, macOS, and Linux. However, the experiment scripts are primarily designed for Linux, and macOS support is limited. You may encounter issues and might not be able to fully reproduce our experiments without Linux.
