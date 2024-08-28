using System.Net;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentConfigs;
public class ExperimentConfig {
    public required IReadOnlyList<IPAddress> PartyIPAddresses { get; set; }

    public ExperimentConfig() { }
}
