import pickle
import matplotlib
import seaborn as sns
import numpy as np
import matplotlib.pyplot as plt

from config import SAVE_FIG_FORMAT

import analyze_data

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

    exp23_compute_data = exp_data["exp23_compute_data"]["timecost"]
    exp23_zkp_data = exp_data["exp23_zkp_data"]["timecost"]

    methods = [
        "Total",
        "Instruction Fetch",
        "Memory Fetch",
        "Instruction Execution",
        "Trace Sort",
        "Trace Verification",
    ]
    method_original_names = ["total", "IF", "MF", "IE", "TS", "TV"]

    subfig_1_x_values = ["Mul.", "Comp.", "Hash", "Store", "Load"]
    if SAVE_FIG_FORMAT == "pgf":
        subfig_1_x_values = [
            "\\scalebox{.9}[1.0]{\\texttt{" + name + "}}" for name in subfig_1_x_values
        ]
    subfig_1_x_original_names = analyze_data.EXP2_INSTANCES

    subfig_2_x_values = [
        4,
        16,
        20,
        32,
        50,
        64,
    ]
    subfig_2_x_value_original_names = analyze_data.EXP3_INSTANCES

    setups = ["8 Parties"]
    setup_original_names = ["mpc-8t"]

    # setups = ["Single Party", "2 Parties", "4 Parties", "8 Parties"]
    # setup_original_names = ["single", "mpc-2t", "mpc-4t", "mpc-8t"]

    y_values = {}

    for setup_original_name, setup in zip(setup_original_names, setups):
        for subfig_index, x_value_original_names, x_values in zip(
            [0, 1],
            [subfig_1_x_original_names, subfig_2_x_value_original_names],
            [subfig_1_x_values, subfig_2_x_values],
        ):
            y_values[subfig_index] = {}
            for x_value_original_name, x_value in zip(x_value_original_names, x_values):
                y_values[subfig_index][x_value] = {}
                for method_original_name, method in zip(method_original_names, methods):
                    if method_original_name == "total":
                        y_values[subfig_index][x_value][method] = exp23_compute_data[
                            f"{setup_original_name}.{x_value_original_name}"
                        ][method_original_name]
                    elif method_original_name in ["TV", "TS"]:
                        y_values[subfig_index][x_value][method] = exp23_compute_data[
                            f"{setup_original_name}.{x_value_original_name}"
                        ][
                            f"{method_original_name}-{analyze_data.EXP23_STEP_COUNTS[x_value_original_name]-1}"
                        ]
                    elif method_original_name in ["IF", "MF", "IE"]:
                        values = None
                        for i in range(
                            analyze_data.EXP23_STEP_COUNTS[x_value_original_name]
                        ):
                            new_values = exp23_compute_data[
                                f"{setup_original_name}.{x_value_original_name}"
                            ][f"{method_original_name}-{i}"]
                            if values == None:
                                values = new_values
                            else:
                                assert len(values) == len(new_values)
                                for j in range(len(values)):
                                    values[j] += new_values[j]
                        y_values[subfig_index][x_value][method] = values
                    else:
                        assert False

    xlabels = ["Instruction type", "Instruction count"]

    fig, axes = plt.subplots(nrows=1, ncols=2, figsize=(7, 4))

    # Legend placeholders
    lines = []
    labels = []

    for ix, x_values, xlabel in zip(
        [0, 1], [subfig_1_x_values, subfig_2_x_values], xlabels
    ):
        ax = axes[ix]

        max_mean_value = 0

        # For each method, plot the line with error bars
        for method in methods:
            # Extract the mean and standard deviation of time costs for each x_value
            mean_values = [np.mean(y_values[ix][x_val][method]) for x_val in x_values]
            error_values = [np.std(y_values[ix][x_val][method]) for x_val in x_values]

            max_mean_value = max(max_mean_value, max(mean_values))

            # Plot the line for the current method
            line, caps, barlinecols = ax.errorbar(
                x_values,
                mean_values,
                yerr=error_values,
                label=method,
                fmt="o-",
                capsize=5,
                alpha=0.9,
            )

            for i, txt in enumerate(mean_values):
                if method == "Total":
                    xytext = (0, 5)
                    if ix in [0, 1] and i == 0:
                        xytext = (3, 5)
                    elif ix in [0] and i == len(mean_values) - 1:
                        xytext = (-3, 5)
                    elif ix in [1] and i == len(mean_values) - 1:
                        xytext = (-8, 5)
                    elif ix == 1 and i in [1]:
                        xytext = (-10, 5)
                    elif ix == 1 and i in [3]:
                        xytext = (-8, 8)
                    ax.annotate(
                        f"{txt:.2f}",
                        (x_values[i], mean_values[i]),
                        textcoords="offset points",
                        xytext=xytext,
                        ha="center",
                        fontsize=9,
                    )

            # Prepare for the shared legend
            if ix == 0:  # Only add to legend list once
                lines.append(line)
                labels.append(method)

        subfig_id = chr(ord("a") + ix)
        ax.set_title(f"({subfig_id})")

        ax.set_ylim(top=max_mean_value * 1.1)

        # Labeling the plot
        ax.set_xlabel(xlabel)
        if ix == 0:
            ax.set_ylabel("Time cost (s)")

        # Ensuring every x value is shown on the x-axis
        ax.set_xticks([x for x in x_values if x != 20])  # Set x-axis ticks

        # Rotate the x-axis labels
        # ax.set_xticklabels(x_values, rotation=45)

        # Optionally, set custom labels (comment this line if unnecessary)
        # ax.set_xticklabels([str(x) for x in x_values])

    # Creating a legend outside the plots
    legend = fig.legend(
        handles=lines,
        labels=labels,
        loc="upper center",
        bbox_to_anchor=(0.48, 0.22),
        ncol=len(methods) // 3,
        # title="zkVM Circuits",
    )

    ## Default:
    ## The figure subplot parameters.  All dimensions are a fraction of the figure width and height.
    # figure.subplot.left:   0.125  # the left side of the subplots of the figure
    # figure.subplot.right:  0.9    # the right side of the subplots of the figure
    # figure.subplot.bottom: 0.11   # the bottom of the subplots of the figure
    # figure.subplot.top:    0.88   # the top of the subplots of the figure
    # figure.subplot.wspace: 0.2    # the amount of width reserved for space between subplots,
    # expressed as a fraction of the average axis width
    # figure.subplot.hspace: 0.2    # the amount of height reserved for space between subplots,
    # expressed as a fraction of the average axis height

    plt.subplots_adjust(
        top=0.88, bottom=0.35, wspace=0.25
    )  # Adjust bottom to increase space

    # plt.show() # Display the plot
    plt.savefig(f"exp23_1.{SAVE_FIG_FORMAT}", bbox_inches="tight")
