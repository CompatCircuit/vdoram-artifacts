using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.GlobalConfig;

namespace SadPencil.CompatCircuitCore;
public static class Startup {
    public static void InitializeJsonSerializer() {
        // This is a workaround for the fact that some static constructors are not called
        ArithConfig.Initialize();
        R1csConstraintJsonConverter.Initialize();
    }
}
