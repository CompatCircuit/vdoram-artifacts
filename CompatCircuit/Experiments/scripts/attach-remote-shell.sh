#!/usr/bin/env bash
set -euo pipefail

source -- ./common.sh
ensure_script_dir

source -- ./config.sh

[ $# -ge 1 ] || (echo Please input the node index. 1>&2 && exit 1)

i="$1"
require_positive_integer $((i + 1))

ssh_user_host="${NODE_SSH_USERNAME}@${NODE_IPS[$i]}"
ssh "$ssh_user_host"
