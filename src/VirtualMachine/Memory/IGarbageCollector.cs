// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Interface for garbage collection in the Tutel VM.
/// </summary>
public interface IGarbageCollector
{
    /// <summary>
    /// Allocates a new array of the specified size.
    /// May trigger garbage collection if needed.
    /// </summary>
    /// <param name="size">The number of elements in the array.</param>
    /// <returns>A tagged handle (index) to the allocated array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when size is negative.</exception>
    long Allocate(int size);

    /// <summary>
    /// Gets the array associated with the given handle.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <returns>The array.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle is invalid or dead.</exception>
    long[] GetArray(int handle);

    /// <summary>
    /// Gets the length of the array associated with the given handle.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <returns>The array length.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle is invalid or dead.</exception>
    int GetArrayLength(int handle);

    /// <summary>
    /// Gets an element from an array.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <param name="index">The element index.</param>
    /// <returns>The element value.</returns>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle or index is invalid.</exception>
    long GetElement(int handle, int index);

    /// <summary>
    /// Sets an element in an array.
    /// </summary>
    /// <param name="handle">The array handle (untagged).</param>
    /// <param name="index">The element index.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when handle or index is invalid.</exception>
    void SetElement(int handle, int index, long value);

    /// <summary>
    /// Runs garbage collection, freeing unreachable arrays.
    /// </summary>
    /// <param name="memory">The memory manager providing root references.</param>
    void Collect(MemoryManager memory);

    /// <summary>
    /// Clears all allocated arrays.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of allocated (alive) arrays.
    /// </summary>
    int ArrayCount { get; }
}
