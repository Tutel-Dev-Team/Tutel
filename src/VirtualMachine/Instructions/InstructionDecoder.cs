// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;

namespace Tutel.VirtualMachine.Instructions;

/// <summary>
/// Decodes bytecode instructions.
/// </summary>
public static class InstructionDecoder
{
    /// <summary>
    /// Decodes the instruction at the specified position in bytecode.
    /// </summary>
    /// <param name="bytecode">The bytecode array.</param>
    /// <param name="pc">The program counter (byte offset).</param>
    /// <returns>The decoded instruction.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when unknown opcode encountered.</exception>
    public static DecodedInstruction Decode(byte[] bytecode, int pc)
    {
        if (pc < 0 || pc >= bytecode.Length)
        {
            throw new System.InvalidOperationException($"Program counter out of range: {pc}");
        }

        byte opcodeByte = bytecode[pc];
        var opcode = (Opcode)opcodeByte;

        return opcode switch
        {
            // 1-byte instructions
            Opcode.Nop or
            Opcode.Pop or
            Opcode.Dup or
            Opcode.Add or
            Opcode.Sub or
            Opcode.Mul or
            Opcode.Div or
            Opcode.Mod or
            Opcode.Neg or
            Opcode.CmpEq or
            Opcode.CmpNe or
            Opcode.CmpLt or
            Opcode.CmpLe or
            Opcode.CmpGt or
            Opcode.CmpGe or
            Opcode.Ret or
            Opcode.ArrayNew or
            Opcode.ArrayLoad or
            Opcode.ArrayStore or
            Opcode.ArrayLen or
            Opcode.Halt
                => new DecodedInstruction(opcode, 1),

            // 2-byte instructions (opcode + uint8)
            Opcode.LoadLocal or
            Opcode.StoreLocal
                => DecodeWithByte(bytecode, pc, opcode),

            // 3-byte instructions (opcode + uint16)
            Opcode.Call or
            Opcode.LoadGlobal or
            Opcode.StoreGlobal
                => DecodeWithUInt16(bytecode, pc, opcode),

            // 5-byte instructions (opcode + int32)
            Opcode.Jmp or
            Opcode.Jz or
            Opcode.Jnz
                => DecodeWithInt32(bytecode, pc, opcode),

            // 9-byte instructions (opcode + int64)
            Opcode.PushInt
                => DecodeWithInt64(bytecode, pc, opcode),

            _ => throw new System.InvalidOperationException($"Unknown opcode: 0x{opcodeByte:X2} at PC={pc}"),
        };
    }

    private static DecodedInstruction DecodeWithByte(byte[] bytecode, int pc, Opcode opcode)
    {
        EnsureBytes(bytecode, pc, 2, opcode);
        return new DecodedInstruction(opcode, 2) { ByteArg = bytecode[pc + 1] };
    }

    private static DecodedInstruction DecodeWithUInt16(byte[] bytecode, int pc, Opcode opcode)
    {
        EnsureBytes(bytecode, pc, 3, opcode);
        ushort value = System.BitConverter.ToUInt16(bytecode, pc + 1);
        return new DecodedInstruction(opcode, 3) { UInt16Arg = value };
    }

    private static DecodedInstruction DecodeWithInt32(byte[] bytecode, int pc, Opcode opcode)
    {
        EnsureBytes(bytecode, pc, 5, opcode);
        int value = System.BitConverter.ToInt32(bytecode, pc + 1);
        return new DecodedInstruction(opcode, 5) { Int32Arg = value };
    }

    private static DecodedInstruction DecodeWithInt64(byte[] bytecode, int pc, Opcode opcode)
    {
        EnsureBytes(bytecode, pc, 9, opcode);
        long value = System.BitConverter.ToInt64(bytecode, pc + 1);
        return new DecodedInstruction(opcode, 9) { Int64Arg = value };
    }

    private static void EnsureBytes(byte[] bytecode, int pc, int needed, Opcode opcode)
    {
        if (pc + needed > bytecode.Length)
        {
            throw new System.InvalidOperationException(
                $"Incomplete instruction {opcode} at PC={pc}: need {needed} bytes, have {bytecode.Length - pc}");
        }
    }
}
