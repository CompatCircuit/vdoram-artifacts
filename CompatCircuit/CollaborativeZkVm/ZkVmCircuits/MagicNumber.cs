using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.GlobalConfig;

namespace SadPencil.CollaborativeZkVm.ZkVmCircuits;
public static class MagicNumber {
    /// <summary>
    /// H(magic, enc_key)
    /// </summary>
    public static Field HashEncKeyMagic { get; } = ArithConfig.FieldFactory.New(1);

    /// <summary>
    /// H(magic, global_step_counter, program_counter, enc_key)
    /// </summary>
    public static Field HashProgramCounterMagic { get; } = ArithConfig.FieldFactory.New(2);

    /// <summary>
    /// H(magic, global_step_counter, reg0, reg1, ..., reg{RegCount - 1}, enc_key)
    /// </summary>
    public static Field HashRegistersMagic { get; } = ArithConfig.FieldFactory.New(3);

    /// <summary>
    /// H(magic, global_step_counter, opcode_table_i_op, opcode_table_i_arg0, opcode_table_i_arg1, opcode_table_i_arg2, enc_key)
    /// </summary>
    public static Field HashOptableRowMagic { get; } = ArithConfig.FieldFactory.New(4);

    /// <summary>
    /// H(magic, global_step_counter, trace_is_mem_op, trace_is_mem_write, trace_mem_addr, trace_mem_val, enc_key)<br/>
    /// Note: special rule: trace_mem_addr and trace_mem_val are output values if is_mem_write, otherwise they are input values
    /// </summary>
    public static Field HashMemTraceMagic { get; } = ArithConfig.FieldFactory.New(9);
}
