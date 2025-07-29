namespace Anonymous.CompatCircuitProgramming;
public static class Startup {
    public static void InitializeJsonSerializer() =>
        // This is a workaround for the fact that some static constructors are not called
        CompatCircuitCore.Startup.InitializeJsonSerializer();
}
