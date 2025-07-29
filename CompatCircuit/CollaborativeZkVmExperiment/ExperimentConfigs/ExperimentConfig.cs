using System.Net;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentConfigs;
public class ExperimentConfig {
    public required IReadOnlyList<IPAddress> PartyIPAddresses { get; set; }

    public ExperimentConfig() { }
}
