﻿using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentOneExecutors;
public class ExperimentOneExecuteResult {
    public required IReadOnlyDictionary<string, R1csCircuitWithValues> R1csCircuitsWithValues { get; init; }
    public required TimeSpan TotalTime { get; init; }
    public required IReadOnlyDictionary<string, TimeSpan> StepTimes { get; init; }
}
