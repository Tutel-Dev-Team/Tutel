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
            Opcode.Nop or Opcode.Pop or Opcode.Dup or Opcode.Add or Opcode.Sub or Opcode.Mul or Opcode.Div or Opcode.Mod
                or Opcode.Neg or Opcode.DAdd or Opcode.DSub or Opcode.DMul or Opcode.DDiv or Opcode.DMod
                or Opcode.DNeg or Opcode.DSqrt or Opcode.I2D or Opcode.CmpEq or Opcode.CmpNe or Opcode.CmpLt
                or Opcode.CmpLe or Opcode.CmpGt or Opcode.CmpGe or Opcode.DCmpEq or Opcode.DCmpNe
                or Opcode.DCmpLt or Opcode.DCmpLe or Opcode.DCmpGt or Opcode.DCmpGe or Opcode.Ret
                or Opcode.ArrayNew or Opcode.ArrayLoad or Opcode.ArrayStore or Opcode.ArrayLen
                or Opcode.PrintInt or Opcode.PrintDouble or Opcode.ReadInt or Opcode.Halt => 1,

            // 2-byte instructions (opcode + uint8)
            Opcode.LoadLocal or Opcode.StoreLocal => 2,

            // 3-byte instructions (opcode + uint16)
            Opcode.Call or Opcode.LoadGlobal or Opcode.StoreGlobal => 3,

            // 5-byte instructions (opcode + int32)
            Opcode.Jmp or Opcode.Jz or Opcode.Jnz => 5,

            // 9-byte instructions (opcode + int64)
            Opcode.PushInt or Opcode.PushDouble => 9,
            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, $"Unknown opcode: 0x{(byte)opcode:X2}"),
        };
    }
}
