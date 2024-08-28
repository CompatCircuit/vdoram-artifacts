from analyze_data import get_exp1_compute_stage_data, get_exp23_compute_stage_data

if __name__ == "__main__":
    exp1_data = get_exp1_compute_stage_data()
    exp23_data = get_exp23_compute_stage_data()

    print("declare -A usage_data")
    for key_name, value_dicts in exp1_data["preshare_usage"].items():
        for preshare_name, usage_count in value_dicts.items():
            print(
                f"usage_data[{key_name.replace('.', '_')}_{preshare_name}]={usage_count}"
            )

    for key_name, value_dicts in exp23_data["preshare_usage"].items():
        for preshare_name, usage_count in value_dicts.items():
            print(
                f"usage_data[{key_name.replace('.', '_')}_{preshare_name}]={usage_count}"
            )
