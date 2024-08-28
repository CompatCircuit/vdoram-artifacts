import pickle
from typing import Dict, List
import numpy as np

import analyze_data

if __name__ == "__main__":
    exp1_preprocess_data = analyze_data.get_exp1_preprocess_stage_data()
    exp23_preprocess_data = analyze_data.get_exp23_preprocess_stage_data()

    exp1_compute_data = analyze_data.get_exp1_compute_stage_data()
    exp23_compute_data = analyze_data.get_exp23_compute_stage_data()

    exp1_zkp_data = analyze_data.get_exp1_zkp_stage_data()
    exp23_zkp_data = analyze_data.get_exp23_zkp_stage_data()

    exp_data = {
        "exp1_preprocess_data": exp1_preprocess_data,
        "exp23_preprocess_data": exp23_preprocess_data,
        "exp1_compute_data": exp1_compute_data,
        "exp23_compute_data": exp23_compute_data,
        "exp1_zkp_data": exp1_zkp_data,
        "exp23_zkp_data": exp23_zkp_data,
    }

    with open("exp_data.pkl", "wb") as file:
        pickle.dump(exp_data, file)

    # print("==== EXP 1 preprocess timecost ====")
    # for setup_name, value_dicts in exp1_preprocess_data["timecost"].items():
    #     for method_name, values in value_dicts.items():
    #         print(f"==== {setup_name}-{method_name} ====")
    #         mean_value = np.mean(values)
    #         max_value = np.max(values)
    #         min_value = np.min(values)
    #         std_dev = np.std(values)

    #         print("Mean: {0:.6f}".format(mean_value))
    #         print("Max: {0:.6f}".format(max_value))
    #         print("Min: {0:.6f}".format(min_value))
    #         print("Standard Deviation: {0:.6f}".format(std_dev))
    #         print()

    # print("==== EXP 23 preprocess timecost ====")
    # for setup_name, value_dicts in exp1_preprocess_data["timecost"].items():
    #     for method_name, values in value_dicts.items():
    #         print(f"==== {setup_name}-{method_name} ====")
    #         mean_value = np.mean(values)
    #         max_value = np.max(values)
    #         min_value = np.min(values)
    #         std_dev = np.std(values)

    #         print("Mean: {0:.6f}".format(mean_value))
    #         print("Max: {0:.6f}".format(max_value))
    #         print("Min: {0:.6f}".format(min_value))
    #         print("Standard Deviation: {0:.6f}".format(std_dev))
    #         print()

    # print("==== EXP 1 compute timecost ====")
    # for setup_name, value_dicts in exp1_compute_data["timecost"].items():
    #     for method_name, values in value_dicts.items():
    #         print(f"==== {setup_name}-{method_name} ====")
    #         mean_value = np.mean(values)
    #         max_value = np.max(values)
    #         min_value = np.min(values)
    #         std_dev = np.std(values)

    #         print("Mean: {0:.6f}".format(mean_value))
    #         print("Max: {0:.6f}".format(max_value))
    #         print("Min: {0:.6f}".format(min_value))
    #         print("Standard Deviation: {0:.6f}".format(std_dev))
    #         print()

    # print("==== EXP 2 compute timecost ====")
    # for setup_name, value_dicts in exp23_compute_data["timecost"].items():
    #     for method_name, values in value_dicts.items():
    #         print(f"==== {setup_name}-{method_name} ====")
    #         mean_value = np.mean(values)
    #         max_value = np.max(values)
    #         min_value = np.min(values)
    #         std_dev = np.std(values)

    #         print("Mean: {0:.6f}".format(mean_value))
    #         print("Max: {0:.6f}".format(max_value))
    #         print("Min: {0:.6f}".format(min_value))
    #         print("Standard Deviation: {0:.6f}".format(std_dev))
    #         print()

    # print("==== EXP 1 zkp timecost ====")
    # for setup_name, method_dict in exp1_zkp_data["timecost"].items():
    #     for method_name, zkp_dict in method_dict.items():
    #         for zkp_step_name, values in zkp_dict.items():
    #             print(f"==== {setup_name}-{method_name}-{zkp_step_name} ====")
    #             mean_value = np.mean(values)
    #             max_value = np.max(values)
    #             min_value = np.min(values)
    #             std_dev = np.std(values)

    #             print("Mean: {0:.6f}".format(mean_value))
    #             print("Max: {0}".format(max_value))
    #             print("Min: {0}".format(min_value))
    #             print("Standard Deviation: {0:.6f}".format(std_dev))
    #             print()

    # print("==== EXP 23 zkp timecost ====")
    # for setup_name, method_dict in exp23_zkp_data["timecost"].items():
    #     for method_name, zkp_dict in method_dict.items():
    #         for zkp_step_name, values in zkp_dict.items():
    #             print(f"==== {setup_name}-{method_name}-{zkp_step_name} ====")
    #             mean_value = np.mean(values)
    #             max_value = np.max(values)
    #             min_value = np.min(values)
    #             std_dev = np.std(values)

    #             print("Mean: {0:.6f}".format(mean_value))
    #             print("Max: {0}".format(max_value))
    #             print("Min: {0}".format(min_value))
    #             print("Standard Deviation: {0:.6f}".format(std_dev))
    #             print()