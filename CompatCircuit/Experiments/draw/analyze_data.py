import re
import os
import glob
import sys
from typing import Dict, List

RAW_DATA_DIR = "rawdata"
COMPUTE_DATA_DIR = os.path.join(RAW_DATA_DIR, "compute")
PREPROCESS_DATA_DIR = os.path.join(RAW_DATA_DIR, "preprocess")
ZKP_DATA_DIR = os.path.join(RAW_DATA_DIR, "zkp")

EXP1_SETUPS = [
    ("single", "exp1_single", 1),
    ("mpc-2t", "exp1_mpc_thread", 2),
    ("mpc-4t", "exp1_mpc_thread", 4),
    ("mpc-8t", "exp1_mpc_thread", 8),
    ("mpc-16t", "exp1_mpc_thread", 16),
]

EXP23_SETUPS = [
    ("single", "exp23_single", 1),
    ("mpc-2t", "exp23_mpc_thread", 2),
    ("mpc-4t", "exp23_mpc_thread", 4),
    ("mpc-8t", "exp23_mpc_thread", 8),
]

EXP2_INSTANCES = [
    "exp2_1",
    "exp2_2",
    "exp2_3",
    "exp2_4",
    "exp2_5",
]

EXP3_INSTANCES = [
    "exp3_4",
    "exp3_16",
    "exp3_20",
    "exp3_32",
    "exp3_50",
    "exp3_64",
]

EXP23_INSTANCES = [
    "exp2_1",
    "exp2_2",
    "exp2_3",
    "exp2_4",
    "exp2_5",
    "exp3_4",
    "exp3_16",
    "exp3_20",
    "exp3_32",
    "exp3_50",
    "exp3_64",
]


EXP1_METHODS = [
    "Addition-100000",
    "Multiplication-100000",
    "Inversion-1000",
    "BitDecomposition-100",
    "zkVM-IE",
]

EXP23_STEP_COUNTS = {
    "exp2_1": 5,
    "exp2_2": 5,
    "exp2_3": 5,
    "exp2_4": 5,
    "exp2_5": 5,
    "exp3_4": 4,
    "exp3_16": 16,
    "exp3_20": 20,
    "exp3_32": 32,
    "exp3_50": 50,
    "exp3_64": 64,
}

EXP23_COMPUTE_METHODS = [
    "IF",
    "MF",
    "IE",
    "TS",
    "TV",
]

EXP23_ZKP_METHODS = [
    "InstructionFetcherCircuit",
    "MemoryTraceProverCircuit",
    "ZkVmCircuit",
]


def get_exp1_preprocess_stage_data() -> Dict[str, Dict[str, Dict[str, List[float]]]]:
    ret = {}
    ret["timecost"] = {}

    # Fetch files
    for setup_name, log_filename_prefix, party_count in EXP1_SETUPS:
        if setup_name == "single":
            continue

        log_files = glob.glob(
            os.path.join(
                PREPROCESS_DATA_DIR,
                f"log.preprocess.{setup_name}.exp1.*.txt",
            )
        )
        if len(log_files) == 0:
            raise Exception(
                "Missing files for pattern "
                + os.path.join(
                    PREPROCESS_DATA_DIR,
                    f"log.preprocess.{setup_name}.exp1.*.txt",
                )
            )

        current_timecosts: Dict[str, List[float]] = {}

        for log_file in log_files:
            timecost_dicts = get_compute_timecost_from_log_file(log_file)
            for method_name, value in timecost_dicts.items():
                if method_name not in current_timecosts:
                    current_timecosts[method_name] = []
                current_timecosts[method_name].append(value)

        ret["timecost"][f"{setup_name}.exp1"] = current_timecosts

    return ret


def get_exp23_preprocess_stage_data() -> Dict[str, Dict[str, Dict[str, List[float]]]]:
    ret = {}
    ret["timecost"] = {}

    # Fetch files
    for setup_name, log_filename_prefix, party_count in EXP23_SETUPS:
        if setup_name == "single":
            continue

        for instance_name in EXP23_INSTANCES:
            log_files = glob.glob(
                os.path.join(
                    PREPROCESS_DATA_DIR,
                    f"log.preprocess.{setup_name}.{instance_name}.*.txt",
                )
            )
            if len(log_files) == 0:
                raise Exception(
                    "Missing files for pattern "
                    + os.path.join(
                        PREPROCESS_DATA_DIR,
                        f"log.preprocess.{setup_name}.{instance_name}.*.txt",
                    )
                )

            current_timecosts: Dict[str, List[float]] = {}

            for log_file in log_files:
                timecost_dicts = get_compute_timecost_from_log_file(log_file)
                for method_name, value in timecost_dicts.items():
                    if method_name not in current_timecosts:
                        current_timecosts[method_name] = []
                    current_timecosts[method_name].append(value)

            ret["timecost"][f"{setup_name}.{instance_name}"] = current_timecosts

    return ret


def get_exp23_compute_stage_data() -> Dict[str, Dict[str, Dict[str, List[float]]]]:
    ret = {}
    ret["timecost"] = {}
    ret["preshare_usage"] = {}
    ret["bytes_per_second"] = {}

    # Fetch files
    for setup_name, log_filename_prefix, party_count in EXP23_SETUPS:
        # We only select time cost from party 0. This is because their time costs are almost same in the same MPC process.
        party_index = 0

        for instance_name in EXP23_INSTANCES:
            log_files = glob.glob(
                os.path.join(
                    COMPUTE_DATA_DIR,
                    f"log-{setup_name}",
                    f"{party_index}",
                    f"log.{log_filename_prefix}.{instance_name}.*.txt",
                )
            )

            if len(log_files) == 0:
                raise Exception(
                    "Missing files for pattern "
                    + os.path.join(
                        COMPUTE_DATA_DIR,
                        f"log-{setup_name}",
                        f"{party_index}",
                        f"log.{log_filename_prefix}.{instance_name}.*.txt",
                    )
                )

            # process timecost
            current_timecosts: Dict[str, List[float]] = {}

            for log_file in log_files:
                preshare_usage = get_preshare_value_usage_from_log_file(log_file)
                # preshare usage should be same among each retries. Will not check for this
                ret["preshare_usage"][f"{setup_name}.{instance_name}"] = preshare_usage

                timecost_dicts = get_compute_timecost_from_log_file(log_file)
                for method_name, value in timecost_dicts.items():
                    if method_name not in current_timecosts:
                        current_timecosts[method_name] = []
                    current_timecosts[method_name].append(value)

            ret["timecost"][f"{setup_name}.{instance_name}"] = current_timecosts

            # process bytes
            if setup_name != "single":
                bytes = []
                for log_file in log_files:
                    bytes.append(
                        get_compute_total_bytes_sent_from_log_file(log_file)
                        / (
                            party_count * (party_count - 1)
                        )  # for each party, compute the per-party bandwidth
                    )
                bytes_per_seconds = []
                for log_file_i in range(len(log_files)):
                    bytes_per_seconds.append(
                        bytes[log_file_i] / current_timecosts["total"][log_file_i]
                    )
                ret["bytes_per_second"][
                    f"{setup_name}.{instance_name}"
                ] = bytes_per_seconds

    return ret


def get_exp1_compute_stage_data() -> Dict[str, Dict[str, Dict[str, List[float]]]]:
    ret = {}
    ret["timecost"] = {}
    ret["preshare_usage"] = {}
    ret["bytes_per_second"] = {}

    # Fetch files
    for setup_name, log_filename_prefix, party_count in EXP1_SETUPS:
        # We only select time cost from party 0. This is because their time costs are almost same in the same MPC process.
        party_index = 0

        log_files = glob.glob(
            os.path.join(
                COMPUTE_DATA_DIR,
                f"log-{setup_name}",
                f"{party_index}",
                f"log.{log_filename_prefix}.*.txt",
            )
        )
        if len(log_files) == 0:
            raise Exception(
                "Missing files for pattern "
                + os.path.join(
                    COMPUTE_DATA_DIR,
                    f"log-{setup_name}",
                    f"{party_index}",
                    f"log.{log_filename_prefix}.*.txt",
                )
            )

        # process timecost
        current_timecosts: Dict[str, List[float]] = {}

        for log_file in log_files:
            preshare_usage = get_preshare_value_usage_from_log_file(log_file)
            # preshare usage should be same among each retries. Will not check for this
            ret["preshare_usage"][f"{setup_name}.exp1"] = preshare_usage

            timecost_dicts = get_compute_timecost_from_log_file(log_file)
            for method_name, value in timecost_dicts.items():
                if method_name not in current_timecosts:
                    current_timecosts[method_name] = []
                current_timecosts[method_name].append(value)

        ret["timecost"][f"{setup_name}.exp1"] = current_timecosts

        # process bytes
        if setup_name != "single":
            bytes = []
            for log_file in log_files:
                bytes.append(
                    get_compute_total_bytes_sent_from_log_file(log_file)
                    / (
                        party_count * (party_count - 1)
                    )  # for each party, compute the per-party bandwidth
                )
            bytes_per_seconds = []
            for log_file_i in range(len(log_files)):
                bytes_per_seconds.append(
                    bytes[log_file_i] / current_timecosts["total"][log_file_i]
                )
            ret["bytes_per_second"][f"{setup_name}.exp1"] = bytes_per_seconds

    return ret


def get_exp1_zkp_stage_data() -> (
    Dict[str, Dict[str, Dict[str, Dict[str, List[float]]]]]
):
    ret = {}
    ret["timecost"] = {}
    ret["bytes_per_second"] = {}

    # Fetch files
    for setup_name, log_filename_prefix, party_count in EXP1_SETUPS:
        # We only select time cost from party 0. This is because their time costs are almost same in the same MPC process.
        party_name = "single" if setup_name == "single" else "party0"
        ret["timecost"][f"{setup_name}.exp1"] = {}
        if setup_name != "single":
            ret["bytes_per_second"][f"{setup_name}.exp1"] = {}

        for method_name in EXP1_METHODS:
            log_files = glob.glob(
                os.path.join(
                    ZKP_DATA_DIR,
                    f"log-{setup_name}",
                    f"{log_filename_prefix}.*.{method_name}.{party_name}.stderr",
                )
            )
            if len(log_files) == 0:
                raise Exception(
                    "Missing files for pattern "
                    + os.path.join(
                        ZKP_DATA_DIR,
                        f"log-{setup_name}",
                        f"{log_filename_prefix}.*.{method_name}.{party_name}.stderr",
                    )
                )

            # process timecost
            current_timecosts = {
                "total": [],
                "setup": [],
                "prove": [],
                "verify": [],
            }
            for log_file in log_files:
                current_timecosts["total"].append(
                    get_zkp_total_timecost_from_stderr_file(log_file)
                )

                for k, v in get_zkp_step_timecost_from_stdout_file(
                    log_file.removesuffix(".stderr") + ".stdout"
                ).items():
                    current_timecosts[k].append(v)

            ret["timecost"][f"{setup_name}.exp1"][method_name] = current_timecosts

            # process bytes
            if setup_name != "single":
                bytes = []
                for log_file in log_files:
                    bytes.append(
                        get_zkp_bytes_sent_from_stderr_file(log_file)
                        / (
                            party_count - 1
                        )  # for each party, compute the per-party bandwidth
                    )
                bytes_per_seconds = []
                for log_file_i in range(len(log_files)):
                    bytes_per_seconds.append(
                        bytes[log_file_i] / current_timecosts["total"][log_file_i]
                    )
                ret["bytes_per_second"][f"{setup_name}.exp1"][
                    method_name
                ] = bytes_per_seconds

    return ret


def get_exp23_zkp_stage_data() -> (
    Dict[str, Dict[str, Dict[str, Dict[str, List[float]]]]]
):
    ret = {}
    ret["timecost"] = {}
    ret["bytes_per_second"] = {}

    # Fetch files
    for setup_name, log_filename_prefix, party_count in EXP23_SETUPS:
        # We only select time cost from party 0. This is because their time costs are almost same in the same MPC process.
        party_name = "single" if setup_name == "single" else "party0"

        for instance_name in EXP23_INSTANCES:
            current_timecosts = {}
            if setup_name != "single":
                ret["bytes_per_second"][f"{setup_name}.{instance_name}"] = {}
            for method_name in EXP23_ZKP_METHODS:
                if method_name == "MemoryTraceProverCircuit":
                    log_files = glob.glob(
                        os.path.join(
                            ZKP_DATA_DIR,
                            f"log-{setup_name}",
                            f"{log_filename_prefix}.{instance_name}.*.{method_name}-{EXP23_STEP_COUNTS[instance_name]}.{party_name}.stderr",
                        )
                    )
                    if len(log_files) == 0:
                        raise Exception(
                            "Missing files for pattern "
                            + os.path.join(
                                ZKP_DATA_DIR,
                                f"log-{setup_name}",
                                f"{log_filename_prefix}.{instance_name}.*.{method_name}-{EXP23_STEP_COUNTS[instance_name]}.{party_name}.stderr",
                            )
                        )

                    # process timecosts
                    current_timecosts[
                        f"{method_name}-{EXP23_STEP_COUNTS[instance_name]}"
                    ] = {
                        "total": [],
                        "setup": [],
                        "prove": [],
                        "verify": [],
                    }
                    for log_file in log_files:
                        current_timecosts[
                            f"{method_name}-{EXP23_STEP_COUNTS[instance_name]}"
                        ]["total"].append(
                            get_zkp_total_timecost_from_stderr_file(log_file)
                        )

                        for k, v in get_zkp_step_timecost_from_stdout_file(
                            log_file.removesuffix(".stderr") + ".stdout"
                        ).items():
                            current_timecosts[
                                f"{method_name}-{EXP23_STEP_COUNTS[instance_name]}"
                            ][k].append(v)

                    # process bytes
                    if setup_name != "single":
                        bytes = []
                        for log_file in log_files:
                            bytes.append(
                                get_zkp_bytes_sent_from_stderr_file(log_file)
                                / (
                                    party_count - 1
                                )  # for each party, compute the per-party bandwidth
                            )
                        bytes_per_seconds = []
                        for log_file_i in range(len(log_files)):
                            bytes_per_seconds.append(
                                bytes[log_file_i]
                                / current_timecosts[
                                    f"{method_name}-{EXP23_STEP_COUNTS[instance_name]}"
                                ]["total"][log_file_i]
                            )
                        ret["bytes_per_second"][f"{setup_name}.{instance_name}"][
                            f"{method_name}-{EXP23_STEP_COUNTS[instance_name]}"
                        ] = bytes_per_seconds

                else:
                    for i in range(EXP23_STEP_COUNTS[instance_name]):
                        log_files = glob.glob(
                            os.path.join(
                                ZKP_DATA_DIR,
                                f"log-{setup_name}",
                                f"{log_filename_prefix}.{instance_name}.*.{method_name}-Step-{i}.{party_name}.stderr",
                            )
                        )
                        if len(log_files) == 0:
                            raise Exception(
                                "Missing files for pattern "
                                + os.path.join(
                                    ZKP_DATA_DIR,
                                    f"log-{setup_name}",
                                    f"{log_filename_prefix}.{instance_name}.*.{method_name}-Step-{i}.{party_name}.stderr",
                                )
                            )

                        current_timecosts[f"{method_name}-Step-{i}"] = {
                            "total": [],
                            "setup": [],
                            "prove": [],
                            "verify": [],
                        }
                        for log_file in log_files:
                            current_timecosts[f"{method_name}-Step-{i}"][
                                "total"
                            ].append(get_zkp_total_timecost_from_stderr_file(log_file))

                            for k, v in get_zkp_step_timecost_from_stdout_file(
                                log_file.removesuffix(".stderr") + ".stdout"
                            ).items():
                                current_timecosts[f"{method_name}-Step-{i}"][k].append(
                                    v
                                )

                        # process bytes
                        if setup_name != "single":
                            bytes = []
                            for log_file in log_files:
                                bytes.append(
                                    get_zkp_bytes_sent_from_stderr_file(log_file)
                                    / (
                                        party_count - 1
                                    )  # for each party, compute the per-party bandwidth
                                )
                            bytes_per_seconds = []
                            for log_file_i in range(len(log_files)):
                                bytes_per_seconds.append(
                                    bytes[log_file_i]
                                    / current_timecosts[f"{method_name}-Step-{i}"][
                                        "total"
                                    ][log_file_i]
                                )
                            ret["bytes_per_second"][f"{setup_name}.{instance_name}"][
                                f"{method_name}-Step-{i}"
                            ] = bytes_per_seconds

            ret["timecost"][f"{setup_name}.{instance_name}"] = current_timecosts

    return ret


def get_zkp_total_timecost_from_stderr_file(filename: str) -> int:
    sys.stderr.write("get_zkp_total_timecost_from_stderr_file:" + filename + "\n")
    # Initialize variables for start and end times
    start_time, end_time = None, None

    with open(filename, "r") as file:
        # Read all lines from the file
        lines = [line.strip() for line in file.readlines()]

        # Filter out any empty lines
        non_empty_lines = list(filter(lambda x: x, lines))

        # Get the first non-empty line (start_time) and last non-empty line (end_time)
        if non_empty_lines:  # Checking if there is at least one non-empty line
            start_time = int(non_empty_lines[0])
            end_time = int(non_empty_lines[-1])

    # Ensure that start_time and end_time have been assigned before subtracting
    if start_time is not None and end_time is not None:
        return end_time - start_time
    else:
        # Raise an exception or return a specific value (e.g., 0) if the file doesn't meet the criteria
        raise ValueError("File must contain at least one non-empty line.")


def get_zkp_bytes_sent_from_stderr_file(file_path: str) -> int:
    sys.stderr.write("get_zkp_bytes_sent_from_stderr_file:" + file_path + "\n")
    # The regex pattern to capture the bytes sent line
    bytes_sent_pattern = re.compile(r"bytes_sent: (\d+),")

    # Start reading from the bottom of the file
    with open(file_path, "rb") as file:
        # Go to the end of the file
        file.seek(0, 2)

        # Position in file
        position = file.tell()
        line = ""

        # Looping backwards to read the file from the end
        while position >= 0:
            file.seek(position)
            char = file.read(1).decode()

            if char == "\n":
                # Process the line
                match = bytes_sent_pattern.search(line.strip())
                if match:
                    total_bytes_sent = int(match.group(1))
                    return total_bytes_sent

                # Reset line and continue
                line = ""
            else:
                line = char + line

            # Move one character back
            position -= 1

        # Check the first line of the file if no newline at the start
        if line:
            match = bytes_sent_pattern.search(line.strip())
            if match:
                total_bytes_sent = int(match.group(1))
                return total_bytes_sent

    # Return an error or zero if no matching pattern found
    raise ValueError("No 'bytes_sent' pattern found in the log file")


def get_preshare_value_usage_from_log_file(file_path: str) -> Dict[str, int]:
    sys.stderr.write("get_preshare_value_usage_from_log_file:" + file_path + "\n")
    # Define the keys we are looking for
    share_keys = [
        "FieldBeaverTripleShare",
        "BoolBeaverTripleShare",
        "EdaBitsKaiShare",
        "DaBitPrioPlusShare",
    ]

    # Initialize the dictionary with None values
    share_values = {key: None for key in share_keys}

    # Some configurations
    chunk_size = 4096  # Size of chunk to read at a time from the end of file

    with open(file_path, "rb") as file:
        # Move to the end of the file
        file.seek(0, 2)
        end_of_file = file.tell()

        # Start reading from the end
        position = end_of_file
        buffer = ""

        while True:
            size_to_read = min(chunk_size, position)
            position -= size_to_read
            file.seek(position)
            data = file.read(size_to_read).decode("utf-8")

            # Prepend new data to cope with any half-read last line from previous chunk
            buffer = data + buffer
            lines = buffer.split("\n")

            # If this chunk does not reach to beginning of file, hold the first item as it might be a partial line
            if position != 0:
                buffer = lines.pop(0)
            else:
                buffer = ""

            # Process each full line
            for line in reversed(
                lines
            ):  # We read the file from the end so we have to reverse lines
                if all(value is not None for value in share_values.values()):
                    # If all values are found, no need to continue processing
                    break
                for key in share_keys:
                    if key in line:
                        # Extract the value and convert it to int
                        try:
                            value = int(line.strip().split()[-1])
                            share_values[key] = value
                        except ValueError:
                            pass
            if position == 0:
                break
        # If a key was not found, default it to 0
        for key in share_keys:
            if share_values[key] is None:
                share_values[key] = 0

    return share_values


def get_compute_total_bytes_sent_from_log_file(file_path: str) -> int:
    sys.stderr.write("get_compute_total_bytes_sent_from_log_file:" + file_path + "\n")
    # The regex pattern to capture necessary log lines
    sent_pattern = re.compile(
        r"Total sent \(all parties\): (\d+) bytes|Total sent: (\d+) bytes"
    )

    # Start reading from the bottom of the file
    with open(file_path, "rb") as file:
        # Go to the end of the file
        file.seek(0, 2)

        # Position in file
        position = file.tell()
        line = ""

        # Looping backwards to read the file from the end
        while position >= 0:
            file.seek(position)
            char = file.read(1).decode()

            if char == "\n":
                # Process the line
                match = sent_pattern.search(line.strip())
                if match:
                    # Extract the first found group which is not None
                    total_bytes_sent = next(
                        (int(num) for num in match.groups() if num is not None), None
                    )
                    if total_bytes_sent is not None:
                        return total_bytes_sent

                # Reset line and continue
                line = ""
            else:
                line = char + line

            # Move one character back
            position -= 1

        # Check the first line of the file if no newline at the start
        if line:
            match = sent_pattern.search(line.strip())
            if match:
                total_bytes_sent = next(
                    (int(num) for num in match.groups() if num is not None), None
                )
                if total_bytes_sent is not None:
                    return total_bytes_sent

    # Return an error or zero if no matching pattern found
    raise ValueError("No matching pattern found in the log file")


def get_compute_timecost_from_log_file(file_path: str) -> Dict[str, float]:
    """
    Reads a file line by line to find specific patterns and stores the findings in a dictionary.

    Args:
    - file_path (str): Path to the text file to be read.

    Returns:
    - dict: A dictionary with method names as keys and their respective durations in seconds as values.
            Includes "total" key for the total time cost.
    """
    sys.stderr.write("get_compute_timecost_from_log_file:" + file_path + "\n")
    results = {}

    with open(file_path, "r") as file:
        read_total_time = False
        read_step_time = False

        for line in file:
            cleaned_line = (
                line.strip().split("]")[1] if "]" in line else line.strip()
            )  # Remove timestamp and "[Information]"

            # Check if we're at "Total time cost" line after removing timestamp and "[Information]"
            if not read_total_time and "Total time cost:" in cleaned_line:
                try:
                    total_time = float(
                        cleaned_line.split("Total time cost:")[1]
                        .split("seconds")[0]
                        .strip()
                    )
                    results["total"] = total_time
                    read_total_time = True
                except ValueError:
                    raise Exception("Error extracting the total time cost.")

            # After finding "Total time cost", check if the next line is about "Step time costs"
            elif read_total_time and not read_step_time:
                if "Step time costs" in cleaned_line:
                    read_step_time = True
                else:
                    break

            # Process each step time cost
            elif read_step_time:
                if ":" in cleaned_line and "seconds" in cleaned_line:
                    try:
                        parts = cleaned_line.split(":")
                        method_name = parts[0].strip()
                        step_time = float(parts[1].split("seconds")[0].strip())
                        results[method_name] = step_time
                    except ValueError:
                        raise Exception("Error extracting step time.")
                else:
                    # Stops reading further once the expected string format is not found
                    break

    return results


def get_zkp_step_timecost_from_stdout_file(filename: str) -> Dict[str, float]:
    sys.stderr.write("get_zkp_step_timecost_from_stdout_file:" + filename + "\n")
    results = {}
    with open(filename, "r") as file:
        for line in file:
            if line.startswith("End:"):
                # Adjust the regular expression to ignore the dots before the time value
                match = re.match(
                    r"End:\s+(.*?)\s*\.*\s*([\d\.]+)(s|ms|µs|ns)", line.strip()
                )
                if match:
                    label = match.group(1)
                    time_value = float(match.group(2))
                    unit = match.group(3)

                    # Convert time based on the unit
                    if unit == "ms":
                        time_value = time_value / 1000
                    elif unit == "µs":
                        time_value = time_value / 1000000
                    elif unit == "ns":
                        time_value = time_value / 1000000000

                    # Add to results
                    results[label] = time_value

    results_classfied = {
        "setup": 0,
        "prove": 0,
        "verify": 0,
    }
    # classify names
    for label, timecost in results.items():
        if label == "Connecting":
            pass
        elif (
            label.startswith("KZG10::Setup")
            or label.startswith("Constructing `powers`")
            or label.startswith("Constructing `shifted_powers`")
            or label == "Committing to polynomials"
        ):
            results_classfied["setup"] += timecost
        elif label in [
            "commit: p",
            "prove_public",
            "prove_gates",
            "prove_wiring",
            "timed section",
        ]:
            results_classfied["prove"] += timecost
        elif label == "Checking evaluations":
            results_classfied["verify"] += timecost
        else:
            raise Exception(f"Unrecognized label {label}")
    return results_classfied
