// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Represents a single stack frame for function execution.
/// </summary>
public sealed class StackFrame
{
    private readonly long[] _localVariables;

    /// <summary>
    /// Initializes a new instance of the <see cref="StackFrame"/> class.
    /// </summary>
    /// <param name="functionIndex">Index of the function being executed.</param>
    /// <param name="returnAddress">Program counter to return to after function completes.</param>
    /// <param name="localVariableCount">Number of local variables for this function.</param>
    public StackFrame(ushort functionIndex, int returnAddress, int localVariableCount)
    {
        FunctionIndex = functionIndex;
        ReturnAddress = returnAddress;
        _localVariables = new long[localVariableCount];
    }

    /// <summary>
    /// Gets the function index for this frame.
    /// </summary>
    public ushort FunctionIndex { get; }

    /// <summary>
    /// Gets the return address (PC to restore after RET).
    /// </summary>
    public int ReturnAddress { get; }

    /// <summary>
    /// Gets the number of local variables in this frame.
    /// </summary>
    public int LocalVariableCount => _localVariables.Length;

    /// <summary>
    /// Gets a local variable by index.
    /// </summary>
    /// <param name="index">The local variable index.</param>
    /// <returns>The value of the local variable.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when index is out of range.</exception>
    public long GetLocal(byte index)
    {
        if (index >= _localVariables.Length)
        {
            throw new System.IndexOutOfRangeException(
                $"Local variable index {index} out of range (function has {_localVariables.Length} locals)");
        }

        return _localVariables[index];
    }

    /// <summary>
    /// Sets a local variable by index.
    /// </summary>
    /// <param name="index">The local variable index.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when index is out of range.</exception>
    public void SetLocal(byte index, long value)
    {
        if (index >= _localVariables.Length)
        {
            throw new System.IndexOutOfRangeException(
                $"Local variable index {index} out of range (function has {_localVariables.Length} locals)");
        }

        _localVariables[index] = value;
    }
}
