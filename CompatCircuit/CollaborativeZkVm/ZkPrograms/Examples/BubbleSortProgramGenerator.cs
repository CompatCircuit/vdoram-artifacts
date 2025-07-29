using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class BubbleSortProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "BubbleSort";
        string programName = "Bubble Sort";
        string programDescription = "Sorts an array of integers in ascending order using the bubble sort algorithm. Input the array length n as public, then input n values as private. The program outputs n sorted values as private.";

        // Modified from https://vaelen.org/post/arm-assembly-sorting/
        List<ZkProgramOpcode> opcodes = [
            // Input
            NewOp(ZkVmOpType.Set, 15, 1), // Line 0: R15 = 1
            NewOp(ZkVmOpType.PublicInput, 1), // Line 1: R1 = array length
            NewOp(ZkVmOpType.Set, 0, 0), // Line 2: R0 = 0
            NewOp(ZkVmOpType.PrivateInput, 2), // Line 3: R2 = array[R0]
            NewOp(ZkVmOpType.Store, 2), // Line 4: memory[R0] = R2
            NewOp(ZkVmOpType.Add, 0, 0, 15), // Line 5: R0 = R0 + 1
            NewOp(ZkVmOpType.LessThan, 2, 0, 1), // Line 6: R2 = bool(R0 < R1)
            NewOp(ZkVmOpType.Not, 2, 2), // Line 7: R2 = NOT R2
            NewOp(ZkVmOpType.JumpIfZero, 2, 3), // Line 8: if R0 < R1, goto Line 3

            // Make a copy of the array length
            NewOp(ZkVmOpType.Move, 8, 1), // Line 9: R8 = R1 (array length)

            // Bubble Sort
            NewOp(ZkVmOpType.Set, 2, 0), // Line 10: R2 = 0
            NewOp(ZkVmOpType.Set, 6, 0), // Line 11: R6 = 0

            NewOp(ZkVmOpType.Add, 3, 2, 15), // Line 12: R3 = R2 + 1
            NewOp(ZkVmOpType.LessThan, 4, 3, 1), // Line 13: R4 = bool(R3 < R1)
            NewOp(ZkVmOpType.JumpIfZero, 4, 29), // Line 14: if R3 >= R1, goto Line 29
            NewOp(ZkVmOpType.Move, 0, 2), // Line 15: R0 = R2
            NewOp(ZkVmOpType.Load, 4), // Line 16: R4 = memory[R0] (memory[R2])
            NewOp(ZkVmOpType.Move, 0, 3), // Line 17: R0 = R3
            NewOp(ZkVmOpType.Load, 5), // Line 18: R5 = memory[R0] (memory[R3])
            NewOp(ZkVmOpType.LessThan, 7, 5, 4), // Line 19: R7 = bool(R5 < R4)
            NewOp(ZkVmOpType.JumpIfZero, 7, 26), // Line 20: if R5 >= R4, goto Line 26

            // Begin swapping: Write swaped elements at index R2 and R3
            NewOp(ZkVmOpType.Move, 0, 2), // Line 21: R0 = R2
            NewOp(ZkVmOpType.Store, 5), // Line 22: memory[R0] = R5 (memory[R2] = R5)
            NewOp(ZkVmOpType.Move, 0, 3), // Line 23: R0 = R3
            NewOp(ZkVmOpType.Store, 4), // Line 24: memory[R0] = R4 (memory[R3] = R4)
            NewOp(ZkVmOpType.Add, 6, 6, 15), // Line 25: R6 = R6 + 1

            // End swapping

            NewOp(ZkVmOpType.Move, 2, 3), // Line 26: R2 = R3
            NewOp(ZkVmOpType.Set, 0, 0), // Line 27: R0 = 0
            NewOp(ZkVmOpType.JumpIfZero, 0, 12), // Line 28: goto Line 12

            NewOp(ZkVmOpType.Set, 0, 0), // Line 29: R0 = 0
            NewOp(ZkVmOpType.LessThan, 0, 0, 6), // Line 30: R0 = bool(0 < R6)
            NewOp(ZkVmOpType.JumpIfZero, 0, 35), // Line 31: if R6 <= 0, goto Line 35
            NewOp(ZkVmOpType.Sub, 1, 1, 15), // Line 32: R1 = R1 - 1
            NewOp(ZkVmOpType.Set, 0, 0), // Line 33: R0 = 0
            NewOp(ZkVmOpType.JumpIfZero, 0, 10), // Line 34: goto Line 10

            // Output
            NewOp(ZkVmOpType.Set, 0, 0), // Line 35: R0 = 0
            NewOp(ZkVmOpType.Load, 2), // Line 36: R2 = memory[R0]
            NewOp(ZkVmOpType.PublicOutput, 2), // Line 37: output R2 = memory[R0]
            NewOp(ZkVmOpType.Add, 0, 0, 15), // Line 38: R0 = R0 + 1
            NewOp(ZkVmOpType.LessThan, 2, 0, 8), // Line 39: R2 = bool(R0 < R8)
            NewOp(ZkVmOpType.Not, 2, 2), // Line 40: R2 = NOT R2
            NewOp(ZkVmOpType.JumpIfZero, 2, 36), // Line 41: if R0 < R8, goto Line 36
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = new List<int>() { 5 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int>() { 114, 514, 1919, 810, 114514 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int>() { 114, 514, 810, 1919, 114514 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count * examplePrivateInputs.Count * examplePrivateInputs.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
