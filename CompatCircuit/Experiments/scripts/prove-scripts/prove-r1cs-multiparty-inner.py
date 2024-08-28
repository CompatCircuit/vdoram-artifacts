import subprocess
import time
import sys
import subprocess
import time
import threading
import os

"""
1. Start a group of processes, redirecting EACH stdout and stderr to files. Count the line count of EACH stdout.
2. After processes are started, wait for 60 seconds, then check if ANY stdout has fewer than 6 lines. If ANY process matches, kill ALL processes at the same time, sleep for 60 seconds, and go to step 1.
3. Since checks have passed, the script continues to wait until ALL processes have ended.
"""

line_numbers = {}


def log_output(index: int, proc, stdout_file):
    line_numbers[index] = 0
    # Use proc.stdout to read output line-by-line
    for line in iter(proc.stdout.readline, b""):
        decoded_line = line.decode()
        # Write each line into the log file with a line number
        stdout_file.write(decoded_line)
        line_numbers[index] += 1
        print(f"[{index}] Line {line_numbers[index]}: {decoded_line.rstrip()}")


def start_processes(party_count: int, r1cs_name: str, r1cs_path: str, bin_client: str):
    processes = []
    stdout_files = []
    stderr_files = []
    logging_threads = []

    for party_index in range(party_count):
        stdout_file = open(os.path.join(r1cs_path, f"{r1cs_name}.party{party_index}.stdout"), "w")
        stderr_file = open(os.path.join(r1cs_path,f"{r1cs_name}.party{party_index}.stderr"), "w")

        stderr_file.write(f"{int(time.time())}\n")
        stderr_file.flush()

        process = subprocess.Popen(
            args=[
                bin_client,
                "-d",
                "PlonkCompatCircuitMultiParty",
                "--hosts",
                f"hosts_{party_count}",
                "--party",
                f"{party_index}",
                f"{r1cs_name}.party{party_index}.r1cs.json",
            ],
            stdout=subprocess.PIPE,
            stderr=stderr_file,
            stdin=subprocess.PIPE,
            cwd=r1cs_path,
        )
        processes.append(process)
        stdout_files.append(stdout_file)
        stderr_files.append(stderr_file)

        # Start a separate thread to handle the output logging
        logging_thread = threading.Thread(
            target=log_output, args=(party_index, process, stdout_file)
        )
        logging_thread.start()
        logging_threads.append(logging_thread)
    return processes, stdout_files, stderr_files, logging_threads


def are_stdout_lines_weird(party_count: int, line_threshold: int):
    for party_index in range(party_count):
        if line_numbers.get(party_index, 0) < line_threshold:
            return True
    return False


def kill_all_processes(processes):
    for process in processes:
        process.kill()


def close_files(files):
    for file in files:
        file.close()


def main():
    if len(sys.argv) < 5:
        print(
            f"Usage: {sys.argv[0]} <party-count> <r1cs-name> <r1cs-path> <bin-client>"
        )
        sys.exit(1)

    party_count = int(sys.argv[1])
    r1cs_name = sys.argv[2]
    r1cs_path = sys.argv[3]
    bin_client = sys.argv[4]

    while True:
        print(f"Starting processes on {r1cs_name}")
        processes, stdout_files, stderr_files, logging_threads = start_processes(
            party_count, r1cs_name, r1cs_path, bin_client
        )
        print(
            f"All processes started. Waiting 60 sec before checking whether they work..."
        )
        time.sleep(60)

        if are_stdout_lines_weird(party_count, line_threshold=6):
            print(f"Failure. Killing processes...")
            kill_all_processes(processes)
            close_files(stdout_files)
            close_files(stderr_files)
            print(f"Waiting 60 sec before restarting them...")
            time.sleep(60)
            continue

        print("Successfully start the clients. Waiting for them to exit...")

        all_done = False
        while not all_done:
            all_done = all(proc.poll() is not None for proc in processes)
            time.sleep(1)
        
        # exited
        for stderr_file in stderr_files:
            stderr_file.write(f"{int(time.time())}\n")
            stderr_file.flush()

        print("Exited. Closing files...")
        close_files(stdout_files)
        close_files(stderr_files)
        break


if __name__ == "__main__":
    main()
