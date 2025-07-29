import pickle
import matplotlib
import seaborn as sns
import numpy as np
import matplotlib.pyplot as plt

from config import SAVE_FIG_FORMAT

EXP1_METHODS = {
    "Addition-100000": "Addition",
    "Multiplication-100000": "Multiplication",
    "Inversion-1000": "Inversion",
    "BitDecomposition-100": "Bit-Decomposition",
    # "zkVM-IE": "Instruction Execution",
}

EXP1_METHODS_UNITS = {
    "Addition": "{\\textmu}s" if SAVE_FIG_FORMAT == "pgf" else "μs",
    "Multiplication": "{\\textmu}s" if SAVE_FIG_FORMAT == "pgf" else "μs",
    "Inversion": "ms",
    "Bit-Decomposition": "ms",
}

EXP1_SETUPS = {
    "single.exp1": 1,
    "mpc-2t.exp1": 2,
    "mpc-4t.exp1": 4,
    "mpc-8t.exp1": 8,
    "mpc-16t.exp1": 16,
}

STAGE_COMPUTE = "Compute"
STAGE_ZKP = "ZKP Prove"

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

    # This is just dummy data for demonstration. In a real scenario, this data would be populated with actual time costs.
    # y_values = {
    #     "Compute": {
    #         "Single Party": {
    #             "Addition": [1, 0.9],
    #             "Multiplication": [1.1, 1],
    #             "Inversion": [0.8, 0.7],
    #             "Bit-Decomposition": [0.9, 0.85],
    #         },
    #         "2 Parties": {
    #             "Addition": [1.5, 1.4],
    #             "Multiplication": [1.6, 1.5],
    #             "Inversion": [1.3, 1.2],
    #             "Bit-Decomposition": [1.4, 1.35],
    #         },
    #         "4 Parties": {
    #             "Addition": [2, 1.9],
    #             "Multiplication": [2.1, 2],
    #             "Inversion": [1.8, 1.7],
    #             "Bit-Decomposition": [1.9, 1.85],
    #         },
    #         "8 Parties": {
    #             "Addition": [2.5, 2.4],
    #             "Multiplication": [2.6, 2.5],
    #             "Inversion": [2.3, 2.2],
    #             "Bit-Decomposition": [2.4, 2.35],
    #         },
    #         "16 Parties": {
    #             "Addition": [3, 2.9],
    #             "Multiplication": [3.1, 3],
    #             "Inversion": [2.8, 2.7],
    #             "Bit-Decomposition": [2.9, 2.85],
    #         },
    #     },
    #     "Prove": {
    #         "Single Party": {
    #             "Addition": [1, 0.9],
    #             "Multiplication": [1.1, 1],
    #             "Inversion": [0.8, 0.7],
    #             "Bit-Decomposition": [0.9, 0.85],
    #         },
    #         "2 Parties": {
    #             "Addition": [1.5, 1.4],
    #             "Multiplication": [1.6, 1.5],
    #             "Inversion": [1.3, 1.2],
    #             "Bit-Decomposition": [1.4, 1.35],
    #         },
    #         "4 Parties": {
    #             "Addition": [2, 1.9],
    #             "Multiplication": [2.1, 2],
    #             "Inversion": [1.8, 1.7],
    #             "Bit-Decomposition": [1.9, 1.85],
    #         },
    #         "8 Parties": {
    #             "Addition": [2.5, 2.4],
    #             "Multiplication": [2.6, 2.5],
    #             "Inversion": [2.3, 2.2],
    #             "Bit-Decomposition": [2.4, 2.35],
    #         },
    #         "16 Parties": {
    #             "Addition": [3, 2.9],
    #             "Multiplication": [3.1, 3],
    #             "Inversion": [2.8, 2.7],
    #             "Bit-Decomposition": [2.9, 2.85],
    #         },
    #     },
    # }

    # methods = ["Addition", "Multiplication", "Inversion", "Bit-Decomposition"]
    methods = list(EXP1_METHODS.values())
    x_values = ["Single", "2", "4", "8", "16"]
    stages = [STAGE_COMPUTE, STAGE_ZKP]

    with open("exp_data.pkl", "rb") as file:
        exp_data = pickle.load(file)

    exp1_compute_data = exp_data["exp1_compute_data"]["timecost"]
    exp1_zkp_data = exp_data["exp1_zkp_data"]["timecost"]

    y_values = {}
    # x_label_original_names = [
    #     "single.exp1",
    #     "mpc-2t.exp1",
    #     "mpc-4t.exp1",
    #     "mpc-8t.exp1",
    #     "mpc-16t.exp1",
    # ]
    x_value_original_names = list(EXP1_SETUPS.keys())
    # method_original_names = [
    #     "Addition-100000",
    #     "Multiplication-100000",
    #     "Inversion-1000",
    #     "BitDecomposition-100",
    # ]
    method_original_names = list(EXP1_METHODS.keys())

    for stage, stage_data in zip(stages, [exp1_compute_data, exp1_zkp_data]):
        y_values[stage] = {}
        for x_value_original_name, x_value in zip(x_value_original_names, x_values):
            y_values[stage][x_value] = {}
            for method_original_name, method in zip(method_original_names, methods):
                if x_value_original_name == "single.exp1" and stage == "Preprocess":
                    y_values[stage][x_value][method] = [0]
                    continue

                if stage == STAGE_COMPUTE:
                    y_values[stage][x_value][method] = stage_data[
                        x_value_original_name
                    ][method_original_name]
                elif stage == STAGE_ZKP:
                    y_values[stage][x_value][method] = stage_data[
                        x_value_original_name
                    ][method_original_name]["prove"]
                else:
                    assert False

                match method_original_name:
                    case "Addition-100000":
                        # s -> us
                        data = [x * 10 for x in y_values[stage][x_value][method]]
                    case "Multiplication-100000":
                        # s -> us
                        data = [x * 10 for x in y_values[stage][x_value][method]]
                    case "Inversion-1000":
                        # s -> ms
                        data = [x for x in y_values[stage][x_value][method]]
                    case "BitDecomposition-100":
                        # s -> ms
                        data = [x * 10 for x in y_values[stage][x_value][method]]

    colors = sns.color_palette()

    # Set up the subplots
    fig, axes = plt.subplots(2, 2, figsize=(7, 7))
    # fig.suptitle("Time Costs by Method across Stages")

    # Legend placeholders
    lines = []
    labels = []

    # Iterate over each subplot/method
    for i, method in enumerate(methods):
        if i in [0, 1]:
            ax = axes[0][i]
        else:
            ax = axes[1][i - 2]
        subfig_id = chr(ord("a") + i)

        ax.set_title(f"({subfig_id}) {method}")

        # Set x-axis to be text
        ax.set_xticks(range(len(x_values)))
        ax.set_xticklabels(x_values)
        ax.set_xlabel("Prover count")

        # Adding y-axis label

        ax.set_ylabel(f"Time cost ({EXP1_METHODS_UNITS[method]})")

        mean_max = 0
        mean_min = None

        # Plot each stage
        for ix, stage in enumerate(stages):
            means = []
            errors = []

            # Collect data for each x-value
            for x_val in x_values:
                data = y_values[stage][x_val][method]
                mean = np.mean(data)
                error = np.std(data)
                means.append(mean)
                errors.append(error)

            mean_max = max(mean_max, max(means))
            mean_min = min(mean_min, min(means)) if mean_min != None else min(means)

            # Plot data with error bars and lines
            line, caps, bars = ax.errorbar(
                range(len(x_values)),
                means,
                yerr=errors,
                fmt="o-",
                capsize=5,
                label=stage,
                color=colors[ix],
                alpha=0.9,
            )

            # Annotate with mean values
            for x, mean in zip(range(len(x_values)), means):
                if stage == STAGE_COMPUTE:
                    if method == "Inversion" and x in [3]:
                        ax.text(
                            x + 0,
                            mean * 1.1 + 4,
                            f"{mean:.1f}",
                            color="#3c3cf0",
                            ha="center",
                            va="bottom",
                            fontsize=9,
                        )
                    else:
                        if x in [4]:
                            ax.text(
                                x - 0.04,
                                mean * 1.3 + 0.01,
                                f"{mean:.1f}",
                                color="#1e1ef0",
                                ha="center",
                                va="bottom",
                                fontsize=9,
                            )
                        else:
                            ax.text(
                                x + 0,
                                mean * 1.3 + 0.01,
                                f"{mean:.1f}",
                                color="#1e1ef0",
                                ha="center",
                                va="bottom",
                                fontsize=9,
                            )
                else:
                    if method == "Inversion" and x in [3]:
                        ax.text(
                            x + 0,
                            mean * 1.25 - 12,
                            f"{mean:.1f}",
                            color="black",
                            ha="center",
                            va="bottom",
                            fontsize=9,
                        )
                    else:
                        if method == "Bit-Decomposition" and x in [4]:
                            ax.text(
                                x - 0.10,
                                mean * 1.25,
                                f"{mean:.1f}",
                                color="black",
                                ha="center",
                                va="bottom",
                                fontsize=9,
                            )
                        elif x in [4]:
                            ax.text(
                                x - 0.04,
                                mean * 1.25,
                                f"{mean:.1f}",
                                color="black",
                                ha="center",
                                va="bottom",
                                fontsize=9,
                            )
                        elif x in [0]:
                            ax.text(
                                x + 0.04,
                                mean * 1.25,
                                f"{mean:.1f}",
                                color="black",
                                ha="center",
                                va="bottom",
                                fontsize=9,
                            )
                        else:
                            ax.text(
                                x + 0,
                                mean * 1.25,
                                f"{mean:.1f}",
                                color="black",
                                ha="center",
                                va="bottom",
                                fontsize=9,
                            )

            # Prepare for the shared legend
            if i == 0:  # Only add to legend list once
                lines.append(line)
                labels.append(stage)

        ax.set_ylim(mean_min / 2, mean_max * 2)

        # Set the y-axis to log scale
        ax.set_yscale("log", base=10)

    # Adjust layout for better fit and visibility
    plt.tight_layout(rect=[0, 0.03, 1, 0.95])

    # Create shared legend outside the rightmost subplot
    fig.legend(
        lines,
        labels,
        loc="upper right",
        bbox_to_anchor=(0.4725, 0.84),  # title="Stages"
    )

    # plt.show() # Display the plot
    plt.savefig(f"exp1.{SAVE_FIG_FORMAT}", bbox_inches="tight")
