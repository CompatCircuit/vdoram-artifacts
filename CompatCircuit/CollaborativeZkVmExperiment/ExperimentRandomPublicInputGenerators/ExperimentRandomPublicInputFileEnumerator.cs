using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentRandomPublicInputGenerators;
public class ExperimentRandomPublicInputFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<Field, Field> {
    public ExperimentRandomPublicInputFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
