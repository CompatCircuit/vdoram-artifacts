using Anonymous.CompatCircuitCore.Arithmetic;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentRandomPublicInputGenerators;
public class ExperimentRandomPublicInputGenerator {
    public required FieldFactory FieldFactory { get; init; }
    public void GenerateExperimentRandomPublicInputFile(Stream stream, int inputCount, bool leaveOpen = false) {
        for (int i = 0; i < inputCount; i++) {
            if (i % 100000 == 0) {
                string percentage = ((double)(i + 1) / inputCount * 100).ToString("F2") + "%";
                string size = ((double)stream.Position / 1048576).ToString("F2") + " MiB";
                Serilog.Log.Information($"[{percentage}] Generate random public inputs ({i + 1}/{inputCount}); {size} written");
                stream.Flush();
            }

            Field value = this.FieldFactory.Random();
            ExperimentRandomPublicInputFileEnumerator.AppendToStream(stream, value);
        }
    }
}
