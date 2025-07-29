namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public static class ZkProgramExamples {
    public static IReadOnlyDictionary<string, ZkProgramExample> Examples { get; }

    static ZkProgramExamples() {
        List<IZkProgramExampleGenerator> exampleZkProgramGenerators = [
            new GreaterThanZkProgramGenerator(),
            new ReverseInputProgramGenerator(),
            new BubbleSortProgramGenerator(),
            new EuclideanProgramGenerator(),
            new FibonacciProgramGenerator(),
            new RangeQueryProgramGenerator(),
            new SlidingWindowProgramGenerator(),
        ];
        Examples = exampleZkProgramGenerators.Select(generator => generator.GetZkProgram()).Select(program => (program.Name, program)).ToDictionary();
    }
}
