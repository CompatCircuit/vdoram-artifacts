import pickle
import numpy as np

if __name__ == "__main__":
    with open("exp_data.pkl", "rb") as file:
        exp_data = pickle.load(file)

    exp1_compute_bandwidth_list = []
    for case_name, values in exp_data["exp1_compute_data"]["bytes_per_second"].items():
        for value in values:
            exp1_compute_bandwidth_list.append(value)
    print("==== EXP 1 compute bandwidth per (party - 1) ====")
    mean_value = np.mean(exp1_compute_bandwidth_list)
    max_value = np.max(exp1_compute_bandwidth_list)
    min_value = np.min(exp1_compute_bandwidth_list)
    std_dev = np.std(exp1_compute_bandwidth_list)
    print("Mean: {0:.6f}".format(mean_value))
    print("Max: {0:.6f}".format(max_value))
    print("Min: {0:.6f}".format(min_value))
    print("Standard Deviation: {0:.6f}".format(std_dev))
    print()

    exp1_zkp_bandwidth_list = []
    for case_name, values in exp_data["exp1_zkp_data"]["bytes_per_second"].items():
        for inner_values in values.values():
            for value in inner_values:
                exp1_zkp_bandwidth_list.append(value)
    print("==== EXP 1 zkp bandwidth per (party - 1) ====")
    mean_value = np.mean(exp1_zkp_bandwidth_list)
    max_value = np.max(exp1_zkp_bandwidth_list)
    min_value = np.min(exp1_zkp_bandwidth_list)
    std_dev = np.std(exp1_zkp_bandwidth_list)
    print("Mean: {0:.6f}".format(mean_value))
    print("Max: {0:.6f}".format(max_value))
    print("Min: {0:.6f}".format(min_value))
    print("Standard Deviation: {0:.6f}".format(std_dev))
    print()

    exp23_compute_bandwidth_list = []
    for case_name, values in exp_data["exp23_compute_data"]["bytes_per_second"].items():
        for value in values:
            exp23_compute_bandwidth_list.append(value)

    print("==== EXP 23 compute bandwidth per (party - 1) ====")
    mean_value = np.mean(exp23_compute_bandwidth_list)
    max_value = np.max(exp23_compute_bandwidth_list)
    min_value = np.min(exp23_compute_bandwidth_list)
    std_dev = np.std(exp23_compute_bandwidth_list)
    print("Mean: {0:.6f}".format(mean_value))
    print("Max: {0:.6f}".format(max_value))
    print("Min: {0:.6f}".format(min_value))
    print("Standard Deviation: {0:.6f}".format(std_dev))
    print()

    exp23_zkp_bandwidth_list = []
    for case_name, values in exp_data["exp23_zkp_data"]["bytes_per_second"].items():
        for inner_values in values.values():
            for value in inner_values:
                exp23_zkp_bandwidth_list.append(value)
    print("==== EXP 23 zkp bandwidth per (party - 1) ====")
    mean_value = np.mean(exp23_zkp_bandwidth_list)
    max_value = np.max(exp23_zkp_bandwidth_list)
    min_value = np.min(exp23_zkp_bandwidth_list)
    std_dev = np.std(exp23_zkp_bandwidth_list)
    print("Mean: {0:.6f}".format(mean_value))
    print("Max: {0:.6f}".format(max_value))
    print("Min: {0:.6f}".format(min_value))
    print("Standard Deviation: {0:.6f}".format(std_dev))
    print()

    all_bandwidth_list = list(
        exp1_compute_bandwidth_list
        + exp1_zkp_bandwidth_list
        + exp23_compute_bandwidth_list
        + exp23_zkp_bandwidth_list
    )
    print("==== all EXP all stages bandwidth per (party - 1) ====")
    mean_value = np.mean(all_bandwidth_list)
    max_value = np.max(all_bandwidth_list)
    min_value = np.min(all_bandwidth_list)
    std_dev = np.std(all_bandwidth_list)
    print("Mean: {0:.6f}".format(mean_value))
    print("Max: {0:.6f}".format(max_value))
    print("Min: {0:.6f}".format(min_value))
    print("Standard Deviation: {0:.6f}".format(std_dev))
    print()
