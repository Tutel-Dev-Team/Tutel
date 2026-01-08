// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Heap storage for dynamically allocated arrays.
/// Uses handle-based access where handles are indices into the internal list.
/// </summary>
public sealed class Heap
{
    private readonly List<long[]> _arrays;

    /// <summary>
    /// Initializes a new instance of the <see cref="Heap"/> class.
    /// </summary>
    public Heap()
    {
        _arrays = new List<long[]>();
    }

    /// <summary>
    /// Gets the number of allocated arrays.
    /// </summary>
    public int ArrayCount => _arrays.Count;

    /// <summary>
    /// Allocates a new array of the specified size.
    /// </summary>
    /// <param name="size">The number of elements in the array.</param>
    /// <returns>A handle (index) to the allocated array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when size is negative.</exception>
    public int AllocateArray(int size)
    {
        if (size < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(size), size, "Array size cannot be negative");
        }

        long[] array = new long[size];
        int handle = _arrays.Count;
        _arrays.Add(array);
        return handle;
    }

    /// <summary>
    /// Gets the array associated with the given handle.
    /// </summary>
    /// <param name="handle">The array handle.</param>
    /// <returns>The array.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle is invalid.</exception>
    public long[] GetArray(int handle)
    {
        ValidateHandle(handle);
        return _arrays[handle];
    }

    /// <summary>
    /// Gets the length of the array associated with the given handle.
    /// </summary>
    /// <param name="handle">The array handle.</param>
    /// <returns>The array length.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle is invalid.</exception>
    public int GetArrayLength(int handle)
    {
        ValidateHandle(handle);
        return _arrays[handle].Length;
    }

    /// <summary>
    /// Gets an element from an array.
    /// </summary>
    /// <param name="handle">The array handle.</param>
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
    /// <param name="handle">The array handle.</param>
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
    }

    private static void ValidateIndex(long[] array, int index)
    {
        if (index < 0 || index >= array.Length)
        {
            throw new System.IndexOutOfRangeException(
                $"Array index {index} out of bounds for array of length {array.Length}");
        }
    }

    private void ValidateHandle(int handle)
    {
        if (handle < 0 || handle >= _arrays.Count)
        {
            throw new System.IndexOutOfRangeException($"Invalid array handle: {handle}");
        }
    }
}
