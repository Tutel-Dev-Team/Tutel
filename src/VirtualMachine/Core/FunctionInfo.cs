// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Represents information about a function in the bytecode module.
/// </summary>
public sealed class FunctionInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInfo"/> class.
    /// </summary>
    /// <param name="index">Function index.</param>
    /// <param name="localVariableCount">Number of local variables.</param>
    /// <param name="bytecode">The function bytecode.</param>
    public FunctionInfo(ushort index, ushort localVariableCount, byte[] bytecode)
    {
        System.ArgumentNullException.ThrowIfNull(bytecode);
        Index = index;
        LocalVariableCount = localVariableCount;
        Bytecode = bytecode;
    }

    /// <summary>
    /// Gets the function index.
    /// </summary>
    public ushort Index { get; }

    /// <summary>
    /// Gets the number of local variables.
    /// </summary>
    public ushort LocalVariableCount { get; }

    /// <summary>
    /// Gets the function bytecode.
    /// </summary>
    public byte[] Bytecode { get; }

    /// <summary>
    /// Gets the bytecode size in bytes.
    /// </summary>
    public int BytecodeSize => Bytecode.Length;
}
