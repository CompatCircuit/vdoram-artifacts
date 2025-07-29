#!/usr/bin/env bash
set -euo pipefail

source -- ./config.sh

exec python3 print-results-inner.py "$R1CS_PATH"