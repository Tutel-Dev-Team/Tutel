// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Managed heap with garbage collection support.
/// Uses mark-and-sweep algorithm to free unreachable arrays.
/// </summary>
public sealed class ManagedHeap : IGarbageCollector
{
    private const int CollectionThreshold = 1000;
    private readonly List<long[]?> _arrays;
    private readonly Queue<int> _freeSlots;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedHeap"/> class.
    /// </summary>
    public ManagedHeap()
    {
        _arrays = new List<long[]?>();
        _freeSlots = new Queue<int>();
    }

    /// <summary>
    /// Gets the number of allocated (alive) arrays.
    /// </summary>
    public int ArrayCount
    {
        get
        {
            int count = 0;
            foreach (long[]? array in _arrays)
            {
                if (array != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Allocates a new array of the specified size.
    /// May trigger garbage collection if threshold is reached.
    /// </summary>
    /// <param name="size">The number of elements in the array.</param>
    /// <returns>A tagged handle (index) to the allocated array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when size is negative.</exception>
    public long Allocate(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Array size cannot be negative");
        }

        int handle;
        long[] array = new long[size];

        // Try to reuse a free slot
        if (_freeSlots.Count > 0)
        {
            handle = _freeSlots.Dequeue();
            _arrays[handle] = array;
        }
        else
        {
            // Allocate new slot
            handle = _arrays.Count;
            _arrays.Add(array);
        }

        return Value.TagAsArray(handle);
    }

    /// <summary>
    /// Gets the array associated with the given handle.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <returns>The array.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle is invalid or dead.</exception>
    public long[] GetArray(int handle)
    {
        ValidateHandle(handle);
        long[]? array = _arrays[handle];
        if (array == null)
        {
            throw new IndexOutOfRangeException($"Invalid or dead array handle: {handle}");
        }

        return array;
    }

    /// <summary>
    /// Gets the length of the array associated with the given handle.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <returns>The array length.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle is invalid or dead.</exception>
    public int GetArrayLength(int handle)
    {
        long[] array = GetArray(handle);
        return array.Length;
    }

    /// <summary>
    /// Gets an element from an array.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <param name="index">The element index.</param>
    /// <returns>The element value.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle or index is invalid.</exception>
    public long GetElement(int handle, int index)
    {
        long[] array = GetArray(handle);
        ValidateIndex(array, index);
        return array[index];
    }

    /// <summary>
    /// Sets an element in an array.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <param name="index">The element index.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle or index is invalid.</exception>
    public void SetElement(int handle, int index, long value)
    {
        long[] array = GetArray(handle);
        ValidateIndex(array, index);
        array[index] = value;
    }

    /// <summary>
    /// Clears all allocated arrays.
    /// </summary>
    public void Clear()
    {
        _arrays.Clear();
        _freeSlots.Clear();
    }

    /// <summary>
    /// Gets all allocated arrays for debugging purposes.
    /// </summary>
    /// <returns>Dictionary mapping handle to array contents.</returns>
    public Dictionary<long, long[]> GetAllArrays()
    {
        var result = new Dictionary<long, long[]>();
        for (int i = 0; i < _arrays.Count; i++)
        {
            long[]? arr = _arrays[i];
            if (arr != null)
            {
                long taggedHandle = Value.TagAsArray(i);
                result[taggedHandle] = arr;
            }
        }

        return result;
    }

    /// <summary>
    /// Runs garbage collection using mark-and-sweep algorithm.
    /// </summary>
    /// <param name="memory">The memory manager providing root references.</param>
    public void Collect(MemoryManager memory)
    {
        var marked = new HashSet<int>();

        // Mark phase - trace from roots
        MarkFromOperandStack(memory.OperandStack, marked);
        MarkFromCallStack(memory.CallStack, marked);
        MarkFromGlobals(memory, marked);

        // Sweep phase - free unmarked arrays
        for (int i = 0; i < _arrays.Count; i++)
        {
            if (_arrays[i] != null && !marked.Contains(i))
            {
                Free(i);
            }
        }
    }

    /// <summary>
    /// Checks if garbage collection should be triggered.
    /// </summary>
    /// <returns>True if GC should run.</returns>
    internal bool ShouldCollect()
    {
        return _arrays.Count >= CollectionThreshold && _freeSlots.Count == 0;
    }

    private static void ValidateIndex(long[] array, int index)
    {
        if (index < 0 || index >= array.Length)
        {
            throw new IndexOutOfRangeException(
                $"Array index {index} out of bounds for array of length {array.Length}");
        }
    }

    /// <summary>
    /// Marks arrays reachable from the operand stack.
    /// </summary>
    private void MarkFromOperandStack(OperandStack stack, HashSet<int> marked)
    {
        foreach (long value in stack.EnumerateValues())
        {
            if (Value.IsArray(value))
            {
                int handle = Value.GetHandle(value);
                MarkRecursive(handle, marked);
            }
        }
    }

    /// <summary>
    /// Marks arrays reachable from the call stack (local variables).
    /// </summary>
    private void MarkFromCallStack(CallStack callStack, HashSet<int> marked)
    {
        foreach (StackFrame frame in callStack.EnumerateFrames())
        {
            foreach (long value in frame.EnumerateLocals())
            {
                if (Value.IsArray(value))
                {
                    int handle = Value.GetHandle(value);
                    MarkRecursive(handle, marked);
                }
            }
        }
    }

    /// <summary>
    /// Marks arrays reachable from global variables.
    /// </summary>
    private void MarkFromGlobals(MemoryManager memory, HashSet<int> marked)
    {
        for (int i = 0; i < memory.GlobalVariableCount; i++)
        {
            long value = memory.GetGlobal((ushort)i);
            if (Value.IsArray(value))
            {
                int handle = Value.GetHandle(value);
                MarkRecursive(handle, marked);
            }
        }
    }

    /// <summary>
    /// Recursively marks an array and all arrays it references.
    /// </summary>
    private void MarkRecursive(int handle, HashSet<int> marked)
    {
        // Already marked or invalid handle
        if (marked.Contains(handle) || handle < 0 || handle >= _arrays.Count || _arrays[handle] == null)
        {
            return;
        }

        marked.Add(handle);
        long[]? array = _arrays[handle];
        if (array == null)
        {
            return;
        }

        // Mark all arrays referenced by elements of this array
        foreach (long element in array)
        {
            if (Value.IsArray(element))
            {
                int childHandle = Value.GetHandle(element);
                MarkRecursive(childHandle, marked);
            }
        }
    }

    /// <summary>
    /// Frees an array slot, making it available for reuse.
    /// </summary>
    private void Free(int handle)
    {
        if (handle >= 0 && handle < _arrays.Count && _arrays[handle] != null)
        {
            _arrays[handle] = null;
            _freeSlots.Enqueue(handle);
        }
    }

    private void ValidateHandle(int handle)
    {
        if (handle < 0 || handle >= _arrays.Count || _arrays[handle] == null)
        {
            throw new IndexOutOfRangeException($"Invalid or dead array handle: {handle}");
        }
    }
}
