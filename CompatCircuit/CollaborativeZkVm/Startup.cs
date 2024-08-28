using SadPencil.CollaborativeZkVm.ZkPrograms;

namespace SadPencil.CollaborativeZkVm;
public static class Startup {
    public static void InitializeJsonSerializer() {
        // This is a workaround for the fact that some static constructors are not called
        ZkProgramOpcodeJsonConverter.Initialize();
        CompatCircuitProgramming.Startup.InitializeJsonSerializer();
        CompatCircuitCore.Startup.InitializeJsonSerializer();
    }
}
