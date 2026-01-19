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
    /// <param name="arity">Number of parameters.</param>
    /// <param name="localVariableCount">Number of local variables (including parameters).</param>
    /// <param name="bytecode">The function bytecode.</param>
    public FunctionInfo(ushort index, byte arity, ushort localVariableCount, byte[] bytecode)
    {
        ArgumentNullException.ThrowIfNull(bytecode);
        Index = index;
        Arity = arity;
        LocalVariableCount = localVariableCount;
        Bytecode = bytecode;
    }

    /// <summary>
    /// Gets the function index.
    /// </summary>
    public ushort Index { get; }

    /// <summary>
    /// Gets the number of parameters (arity).
    /// </summary>
    public byte Arity { get; }

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

    public int CallCount { get; set; }

    public IntPtr NativePtr { get; set; }

    public Delegate? NativeDelegate { get; set; }

    public bool JitFailed { get; set; }

    public int JitCompileCount { get; set; }

    public int JitExecutionCount { get; set; }

    public long JitTotalTicks { get; set; }
}