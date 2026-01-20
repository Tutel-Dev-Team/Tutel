// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Provides size information for opcodes.
/// </summary>
public static class OpcodeInfo
{
    /// <summary>
    /// Gets the total instruction size in bytes for the given opcode.
    /// </summary>
    /// <param name="opcode">The opcode to get size for.</param>
    /// <returns>Size in bytes including the opcode byte.</returns>
    public static int GetInstructionSize(Opcode opcode)
    {
        return opcode switch
        {
            // 1-byte instructions (opcode only)
            Opcode.Nop => 1,
            Opcode.Pop => 1,
            Opcode.Dup => 1,
            Opcode.Add => 1,
            Opcode.Sub => 1,
            Opcode.Mul => 1,
            Opcode.Div => 1,
            Opcode.Mod => 1,
            Opcode.Neg => 1,
            Opcode.CmpEq => 1,
            Opcode.CmpNe => 1,
            Opcode.CmpLt => 1,
            Opcode.CmpLe => 1,
            Opcode.CmpGt => 1,
            Opcode.CmpGe => 1,
            Opcode.Ret => 1,
            Opcode.ArrayNew => 1,
            Opcode.ArrayLoad => 1,
            Opcode.ArrayStore => 1,
            Opcode.ArrayLen => 1,
            Opcode.PrintInt => 1,
            Opcode.ReadInt => 1,
            Opcode.Halt => 1,

            // 2-byte instructions (opcode + uint8)
            Opcode.LoadLocal => 2,
            Opcode.StoreLocal => 2,

            // 3-byte instructions (opcode + uint16)
            Opcode.Call => 3,
            Opcode.LoadGlobal => 3,
            Opcode.StoreGlobal => 3,

            // 5-byte instructions (opcode + int32)
            Opcode.Jmp => 5,
            Opcode.Jz => 5,
            Opcode.Jnz => 5,

            // 9-byte instructions (opcode + int64)
            Opcode.PushInt => 9,

            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, $"Unknown opcode: 0x{(byte)opcode:X2}"),
        };
    }
}
