using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentRandomPublicInputGenerators;
public class ExperimentRandomPublicInputFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<Field, Field> {
    public ExperimentRandomPublicInputFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
