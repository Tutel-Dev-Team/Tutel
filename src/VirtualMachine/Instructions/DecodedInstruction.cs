// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;

namespace Tutel.VirtualMachine.Instructions;

/// <summary>
/// Represents a decoded instruction.
/// </summary>
public readonly struct DecodedInstruction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecodedInstruction"/> struct.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <param name="size">Instruction size in bytes.</param>
    public DecodedInstruction(Opcode opcode, int size)
    {
        Opcode = opcode;
        Size = size;
        Int64Arg = 0;
        Int32Arg = 0;
        UInt16Arg = 0;
        ByteArg = 0;
    }

    /// <summary>
    /// Gets the opcode.
    /// </summary>
    public Opcode Opcode { get; }

    /// <summary>
    /// Gets the instruction size in bytes.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the int64 argument (for PUSH_INT).
    /// </summary>
    public long Int64Arg { get; init; }

    /// <summary>
    /// Gets the int32 argument (for JMP, JZ, JNZ).
    /// </summary>
    public int Int32Arg { get; init; }

    /// <summary>
    /// Gets the uint16 argument (for CALL, LOAD_GLOBAL, STORE_GLOBAL).
    /// </summary>
    public ushort UInt16Arg { get; init; }

    /// <summary>
    /// Gets the byte argument (for LOAD_LOCAL, STORE_LOCAL).
    /// </summary>
    public byte ByteArg { get; init; }
}
