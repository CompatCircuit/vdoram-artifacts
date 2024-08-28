#!/usr/bin/env bash
set -euo pipefail

source -- ./config.sh
source -- ./data.sh
source -- ./exp-constants.sh

if [ "${#EXP1_PARTY_SETUPS[@]}" -ne "${#EXP1_PARTY_COUNTS[@]}" ]; then
    echo "Error: The length of PARTY_SETUPS and PARTY_COUNTS does not match."
    exit 1
fi

if [ "${#EXP23_PARTY_SETUPS[@]}" -ne "${#EXP23_PARTY_COUNTS[@]}" ]; then
    echo "Error: The length of PARTY_SETUPS and PARTY_COUNTS does not match."
    exit 1
fi

for ((repeat = 1; repeat <= $EXP_REPEAT_COUNT; repeat++)); do
    for ((i = 0; i < ${#EXP1_PARTY_SETUPS[@]}; i++)); do
        setup=${EXP1_PARTY_SETUPS[$i]}
        party_count=${EXP1_PARTY_COUNTS[$i]}

        for instance in "${EXP1_INSTANCES[@]}"; do
            echo "Pre-processing setup $setup with experiment instance $instance"
            INSTANCE_NAME=preprocess."$setup"."$instance".repeat"$repeat" "$BIN_ZKVM_EXP" gen-preshared --parties "$party_count" --field-beaver-triples "${usage_data[${setup}_${instance}_FieldBeaverTripleShare]}" --bool-beaver-triples "${usage_data[${setup}_${instance}_BoolBeaverTripleShare]}" --edaBits-pair "${usage_data[${setup}_${instance}_EdaBitsKaiShare]}" --daBitPrioPlus-pair "${usage_data[${setup}_${instance}_DaBitPrioPlusShare]}" --unsafe-use-fake-random-source || true
        done
    done
done

for ((repeat = 1; repeat <= $EXP_REPEAT_COUNT; repeat++)); do
    for ((i = 0; i < ${#EXP23_PARTY_SETUPS[@]}; i++)); do
        setup=${EXP23_PARTY_SETUPS[$i]}
        party_count=${EXP23_PARTY_COUNTS[$i]}

        for instance in "${EXP23_INSTANCES[@]}"; do
            echo "Pre-processing setup $setup with experiment instance $instance"
            INSTANCE_NAME=preprocess."$setup"."$instance".repeat"$repeat" "$BIN_ZKVM_EXP" gen-preshared --parties "$party_count" --field-beaver-triples "${usage_data[${setup}_${instance}_FieldBeaverTripleShare]}" --bool-beaver-triples "${usage_data[${setup}_${instance}_BoolBeaverTripleShare]}" --edaBits-pair "${usage_data[${setup}_${instance}_EdaBitsKaiShare]}" --daBitPrioPlus-pair "${usage_data[${setup}_${instance}_DaBitPrioPlusShare]}" --unsafe-use-fake-random-source || true
        done
    done
done
