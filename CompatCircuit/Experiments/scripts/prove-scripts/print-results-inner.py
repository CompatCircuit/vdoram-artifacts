import json
import re
import os
import glob
import sys
from typing import Dict, List

def get_zkp_step_timecost_from_stdout_file(filename: str) -> Dict[str, float]:
    results = []
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
                    if unit == "s":
                        pass
                    elif unit == "ms":
                        time_value = time_value / 1000
                    elif unit == "µs":
                        time_value = time_value / 1000000
                    elif unit == "ns":
                        time_value = time_value / 1000000000
                    else:
                        raise Exception(f"Unknown unit {unit}")

                    # Add to results
                    results.append((label, time_value))
                else:
                    print(f"unrecognized line: {line}")

    del line
    del time_value
    del unit

    results_classified = {
        "setup": 0,
        "prove": 0,
        "verify": 0,
    }
    # classify names
    for label, timecost in results:
        if label == "Connecting":
            pass
        elif (
            label.startswith("KZG10::Setup")
            or label.startswith("Constructing `powers`")
            or label.startswith("Constructing `shifted_powers`")
            or label == "Committing to polynomials"
        ):
            results_classified["setup"] += timecost
        elif label in [
            "commit: p",
            "prove_public",
            "prove_gates",
            "prove_wiring",
            "timed section",
        ]:
            results_classified["prove"] += timecost
        elif label == "Checking evaluations":
            results_classified["verify"] += timecost
        else:
            raise Exception(f"Unrecognized label {label}")
    return results_classified

def print_high_precision(data: dict[str, float], precision: int = 16) -> None:
    formatted = {k: f"{v:.{precision}f}" for k, v in data.items()}
    print(json.dumps(formatted, indent=4, ensure_ascii=False))

if __name__ == '__main__':
    # Check if a directory was provided
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <directory>")
        sys.exit(1)
    
    # Get the directory from command line argument
    target_dir = sys.argv[1]

    # List all files in the current directory
    for filename in os.listdir(target_dir):
        # Check if the file ends with .stdout
        if filename.endswith('.stdout'):
            filepath = os.path.join(target_dir, filename)
            # Print the full path
            print(filepath)

            # Print the result
            result = get_zkp_step_timecost_from_stdout_file(filepath)
            print_high_precision(result)

            print()