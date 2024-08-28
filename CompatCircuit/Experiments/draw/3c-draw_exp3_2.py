import pickle
import matplotlib
import seaborn as sns
import numpy as np
import matplotlib.pyplot as plt

from config import SAVE_FIG_FORMAT

import numpy as np
import matplotlib.pyplot as plt

STAGE_COMPUTE = "Computation"
STAGE_ZKP = "Proof"

if __name__ == "__main__":
    sns.set_style("whitegrid")

    if SAVE_FIG_FORMAT == "pgf":
        # https://matplotlib.org/stable/tutorials/text/pgf.html
        matplotlib.use("pgf")
        plt.rcParams.update(
            {
                "text.usetex": True,
                "pgf.texsystem": "pdflatex",
                "pgf.preamble": "\n".join(
                    [
                        "\\usepackage[utf8x]{inputenc}",
                        "\\usepackage[T1]{fontenc}",
                        "\\usepackage{cmbright}",
                    ]
                ),
                "pgf.rcfonts": False,
                "font.serif": [],  # use latex default
                "font.sans-serif": [],  # use latex default
                "font.monospace": [],  # use latex default
                "font.family": "serif",
            }
        )
    else:
        plt.rcParams.update(
            {
                "text.usetex": False,
                "font.family": "Times New Roman",
            }
        )

    # https://stackoverflow.com/a/39566040

    SMALL_SIZE = 18  # 8
    MEDIUM_SIZE = 24  # 10
    BIGGER_SIZE = 24  # 12

    plt.rc("font", size=SMALL_SIZE)  # controls default text sizes
    plt.rc("axes", titlesize=SMALL_SIZE)  # fontsize of the axes title
    plt.rc("axes", labelsize=SMALL_SIZE)  # fontsize of the x and y labels
    plt.rc("xtick", labelsize=SMALL_SIZE)  # fontsize of the tick labels
    plt.rc("ytick", labelsize=SMALL_SIZE)  # fontsize of the tick labels
    plt.rc("legend", fontsize=SMALL_SIZE)  # legend fontsize
    plt.rc("figure", titlesize=BIGGER_SIZE)  # fontsize of the figure title

    with open("exp_data.pkl", "rb") as file:
        exp_data = pickle.load(file)

    exp23_preprocess_data = exp_data["exp23_preprocess_data"]["timecost"]
    exp23_compute_data = exp_data["exp23_compute_data"]["timecost"]
    exp23_zkp_data = exp_data["exp23_zkp_data"]["timecost"]

    parties = ["Single", "2", "4", "8"]
    party_original_names = ["single", "mpc-2t", "mpc-4t", "mpc-8t"]
    stages = [STAGE_COMPUTE, STAGE_ZKP]
    computation_methods = ["Preprocess", "Compute"]
    computation_method_original_names = ["preprocess", "compute"]
    proof_methods = ["ZKP Setup", "ZKP Prove", "ZKP Verify"]
    proof_method_original_names = ["setup", "prove", "verify"]

    instance_name = "exp3_64"

    y_datas = {}
    for party, party_original_name in zip(parties, party_original_names):
        y_datas[party] = {}
        for stage in stages:
            if stage == STAGE_COMPUTE:
                methods = computation_methods
                method_original_names = computation_method_original_names
            elif stage == STAGE_ZKP:
                methods = proof_methods
                method_original_names = proof_method_original_names

                zkp_data_sum = None
                for k, v in exp23_zkp_data[
                    f"{party_original_name}.{instance_name}"
                ].items():
                    if zkp_data_sum == None:
                        zkp_data_sum = v
                    else:
                        for method, values in v.items():
                            for i, value in enumerate(values):
                                zkp_data_sum[method][i] += value

            else:
                assert False

            for method, method_original_name in zip(methods, method_original_names):
                if method_original_name == "preprocess":
                    if party_original_name == "single":
                        y_datas[party][method] = [0]
                    else:
                        y_datas[party][method] = exp23_preprocess_data[
                            f"{party_original_name}.{instance_name}"
                        ]["total"]
                elif method_original_name == "compute":
                    y_datas[party][method] = exp23_compute_data[
                        f"{party_original_name}.{instance_name}"
                    ]["total"]
                else:
                    y_datas[party][method] = zkp_data_sum[method_original_name]
    methods = computation_methods + proof_methods

    colors = sns.color_palette()

    fig, ax = plt.subplots(figsize=(7, 5))

    # Bar width
    width = 0.18  # Slight adjustment for better visibility

    # Create a range for the total number of party settings
    positions = np.arange(len(parties))

    max_mean_value = 0
    # Iterate through each method for plotting. Keep methods in original order since we want "preprocess" on top.
    for idx, method in enumerate(methods):
        means = [np.mean(y_datas[party][method]) for party in parties]
        errors = [np.std(y_datas[party][method]) for party in parties]

        max_mean_value = max(max_mean_value, max(means))

        # Calculate position offset for each method
        offset_positions = [p + idx * width for p in positions]

        # Plot each set of bars for the current method
        ax.barh(
            offset_positions,
            means,
            height=width,
            xerr=errors,
            label=method,
            color=colors[idx],
            capsize=3,
            alpha=0.9,
        )

        # Add the mean values on the bars for clarity
        for i, (pos, mean) in enumerate(zip(offset_positions, means)):
            if idx == 0 and i == 0:
                continue  # ignore on single party preprocessing
            ax.text(
                mean * 1.1 + 0.01, pos + 0.03, f"{mean:.2f}", va="center", fontsize=9
            )

    # Adjust y-ticks to be placed at the center of grouped bars
    ax.set_yticks([p + 2 * width for p in positions])
    ax.set_yticklabels(parties)
    ax.set_xscale("log", base=2)
    ax.set_xlim(right=max_mean_value * 4)
    # ax.set_xlim(0, math.log2(max_mean_value))
    ax.invert_yaxis()  # Invert y-axis to have "single" at the top
    ax.set_xlabel("Time cost (s)", fontsize=SMALL_SIZE)
    ax.set_ylabel("Party count", fontsize=SMALL_SIZE)
    # ax.set_title("Performance Metrics Across Different Party Settings and Methods")

    ax.legend(
        loc="upper center",
        bbox_to_anchor=(0.41, -0.25),
        ncol=3,
    )
    plt.subplots_adjust(
        top=0.88, bottom=0.35, wspace=0.2, left=0.2, right=0.98
    )  # Adjust bottom to increase space

    # plt.show()
    plt.savefig(f"exp3_2.{SAVE_FIG_FORMAT}", bbox_inches="tight")
