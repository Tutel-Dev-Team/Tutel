// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Unified memory management for the VM, providing access to all memory areas.
/// </summary>
public sealed class MemoryManager
{
    private readonly long[] _globalVariables;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryManager"/> class.
    /// </summary>
    /// <param name="globalVariableCount">Number of global variables to allocate.</param>
    public MemoryManager(int globalVariableCount)
    {
        if (globalVariableCount < 0 || globalVariableCount > VmLimits.MaxGlobalVariables)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(globalVariableCount),
                globalVariableCount,
                $"Global variable count must be between 0 and {VmLimits.MaxGlobalVariables}");
        }

        OperandStack = new OperandStack();
        CallStack = new CallStack();
        Heap = new Heap();
        _globalVariables = new long[globalVariableCount];
    }

    /// <summary>
    /// Gets the operand stack.
    /// </summary>
    public OperandStack OperandStack { get; }

    /// <summary>
    /// Gets the call stack.
    /// </summary>
    public CallStack CallStack { get; }

    /// <summary>
    /// Gets the heap for array storage.
    /// </summary>
    public Heap Heap { get; }

    /// <summary>
    /// Gets the number of global variables.
    /// </summary>
    public int GlobalVariableCount => _globalVariables.Length;

    /// <summary>
    /// Gets a global variable by index.
    /// </summary>
    /// <param name="index">The global variable index.</param>
    /// <returns>The value of the global variable.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when index is out of range.</exception>
    public long GetGlobal(ushort index)
    {
        ValidateGlobalIndex(index);
        return _globalVariables[index];
    }

    /// <summary>
    /// Sets a global variable by index.
    /// </summary>
    /// <param name="index">The global variable index.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when index is out of range.</exception>
    public void SetGlobal(ushort index, long value)
    {
        ValidateGlobalIndex(index);
        _globalVariables[index] = value;
    }

    /// <summary>
    /// Gets a local variable from the current stack frame.
    /// </summary>
    /// <param name="index">The local variable index.</param>
    /// <returns>The value of the local variable.</returns>
    public long GetLocal(byte index)
    {
        return CallStack.CurrentFrame.GetLocal(index);
    }

    /// <summary>
    /// Sets a local variable in the current stack frame.
    /// </summary>
    /// <param name="index">The local variable index.</param>
    /// <param name="value">The value to store.</param>
    public void SetLocal(byte index, long value)
    {
        CallStack.CurrentFrame.SetLocal(index, value);
    }

    /// <summary>
    /// Resets all memory to initial state.
    /// </summary>
    public void Reset()
    {
        OperandStack.Clear();
        CallStack.Clear();
        Heap.Clear();
        System.Array.Clear(_globalVariables);
    }

    private void ValidateGlobalIndex(ushort index)
    {
        if (index >= _globalVariables.Length)
        {
            throw new System.IndexOutOfRangeException(
                $"Global variable index {index} out of range (have {_globalVariables.Length} globals)");
        }
    }
}
