namespace SadPencil.CollaborativeZkVmExperiment;
public static class Startup {
    public static void InitializeJsonSerializer() {
        // This is a workaround for the fact that some static constructors are not called
        CollaborativeZkVm.Startup.InitializeJsonSerializer();
        CompatCircuitProgramming.Startup.InitializeJsonSerializer();
        CompatCircuitCore.Startup.InitializeJsonSerializer();
    }
}
