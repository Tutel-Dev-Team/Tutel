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
    /// <param name="gc">Optional garbage collector. If null, uses ManagedHeap with GC.</param>
    public MemoryManager(int globalVariableCount, IGarbageCollector? gc = null)
    {
        if (globalVariableCount < 0 || globalVariableCount > VmLimits.MaxGlobalVariables)
        {
            throw new ArgumentOutOfRangeException(
                nameof(globalVariableCount),
                globalVariableCount,
                $"Global variable count must be between 0 and {VmLimits.MaxGlobalVariables}");
        }

        OperandStack = new OperandStack();
        CallStack = new CallStack();
        GC = gc ?? new ManagedHeap();
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
    /// Gets the garbage collector for array storage.
    /// </summary>
    public IGarbageCollector GC { get; }

    /// <summary>
    /// Gets the heap for array storage (legacy compatibility).
    /// </summary>
    [Obsolete("Use GC property instead. This property returns a wrapper for backward compatibility.")]
    public Heap Heap => new HeapWrapper(GC);

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
        GC.Clear();
        Array.Clear(_globalVariables);
    }

    /// <summary>
    /// Runs garbage collection to free unreachable arrays.
    /// </summary>
    public void CollectGarbage()
    {
        GC.Collect(this);
    }

    /// <summary>
    /// Allocates a new array, automatically triggering GC if threshold is reached.
    /// </summary>
    /// <param name="size">The number of elements in the array.</param>
    /// <returns>A tagged handle to the allocated array.</returns>
    internal long AllocateArray(int size)
    {
        // Trigger GC if we're using ManagedHeap and threshold is reached
        if (GC is ManagedHeap managedHeap && managedHeap.ShouldCollect())
        {
            CollectGarbage();
        }

        return GC.Allocate(size);
    }

    private void ValidateGlobalIndex(ushort index)
    {
        if (index >= _globalVariables.Length)
        {
            throw new IndexOutOfRangeException(
                $"Global variable index {index} out of range (have {_globalVariables.Length} globals)");
        }
    }
}
