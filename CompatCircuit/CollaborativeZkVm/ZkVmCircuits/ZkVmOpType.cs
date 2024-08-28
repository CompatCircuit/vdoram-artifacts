namespace SadPencil.CollaborativeZkVm.ZkVmCircuits;
public enum ZkVmOpType : byte {
    /// <summary>
    /// Stop the program
    /// </summary>
    Halt,
    /// <summary>
    /// Set Reg{arg0} = external public input
    /// </summary>
    PublicInput,
    /// <summary>
    /// Set Reg{arg0} = external private input
    /// </summary>
    PrivateInput,
    /// <summary>
    /// Set external public output = Reg{arg0}
    /// </summary>
    PublicOutput,
    /// <summary>
    /// Set Reg{i+1} = Reg{i} for all i. Note: Reg0 = Reg{RegCount - 1}
    /// </summary>
    Shift,
    /// <summary>
    /// Swap Reg{arg0} with Reg{arg1}
    /// </summary>
    Swap,
    /// <summary>
    /// Set Reg{arg0} = Reg{arg1}
    /// </summary>
    Move,
    /// <summary>
    /// Set Reg{arg0} = constant value {arg1}
    /// </summary>
    Set,
    /// <summary>
    /// Do nothing. Program counter increases itself by 1, just like a normal instruction
    /// </summary>
    Noop,
    /// <summary>
    /// Set program counter to constant location {arg1} if Reg{arg0} is zero. Otherwise, program counter increases itself by 1, just like a normal instruction
    /// </summary>
    JumpIfZero,
    /// <summary>
    /// Set external memory value = Reg{arg0}. The memory address is stored in Reg0 (not to be confused with Reg{arg0}).<br/>
    /// Note: memory address NegOne is reserved and should not be used.
    /// </summary>
    Store,
    /// <summary>
    /// Set Reg{arg0} = memory value. The memory address is stored in Reg0 (not to be confused with Reg{arg0}).<br/>
    /// Note: memory address NegOne is reserved and should not be used.
    /// </summary>
    Load,
    /// <summary>
    /// Set Reg{arg0} = MiMCHash(Reg{arg1})
    /// </summary>
    Hash,
    /// <summary>
    /// Set Reg{arg0} = Reg{arg1} + Reg{arg2}
    /// </summary>
    Add,
    /// <summary>
    ///  Set Reg{arg0} = Reg{arg1} - Reg{arg2}
    /// </summary>
    Sub,
    /// <summary>
    ///  Set Reg{arg0} = Reg{arg1} * Reg{arg2}
    /// </summary>
    Mul,
    /// <summary>
    ///  Set Reg{arg0} = the inverse of Reg{arg1}
    /// </summary>
    Inv,
    /// <summary>
    /// Set Reg{arg0} = 1 if Reg{arg1} is not zero, otherwise 0. In other words, convert Reg{arg1} to boolean value and store it in Reg{arg0}
    /// </summary>
    Norm,
    /// <summary>
    /// Set Reg{arg0} = Reg{arg1} AND Reg{arg2}. Note: Reg{arg1} and Reg{arg2} are treated as boolean values (will not check for this)
    /// </summary>
    And,
    /// <summary>
    /// Set Reg{arg0} = Reg{arg1} OR Reg{arg2}. Note: Reg{arg1} and Reg{arg2} are treated as boolean values (will not check for this)
    /// </summary>
    Or,
    /// <summary>
    /// Set Reg{arg0} = Reg{arg1} XOR Reg{arg2}. Note: Reg{arg1} and Reg{arg2} are treated as boolean values (will not check for this)
    /// </summary>
    Xor,
    /// <summary>
    /// Set Reg{arg0} = NOT Reg{arg1}. Note: Reg{arg1} is treated as a boolean value (will not check for this)
    /// </summary>
    Not,
    /// <summary>
    /// Set Reg{arg0} = 1 if Reg{arg1} is less than Reg{arg2}, otherwise 0
    /// </summary>
    LessThan,
}
