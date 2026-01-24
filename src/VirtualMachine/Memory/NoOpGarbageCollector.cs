// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Заглушка сборщика мусора: хранит массивы, но никогда их не освобождает.
/// Нужна для режима --gc=off, чтобы увидеть поведение без управления памятью.
/// </summary>
public sealed class NoOpGarbageCollector : IGarbageCollector
{
    private readonly List<long[]> _arrays = new();

    /// <inheritdoc />
    public int ArrayCount => _arrays.Count;

    /// <inheritdoc />
    public long Allocate(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Array size cannot be negative");
        }

        long[] array = new long[size];
        _arrays.Add(array);
        return Value.TagAsArray(_arrays.Count - 1);
    }

    /// <inheritdoc />
    public long[] GetArray(int handle)
    {
        ValidateHandle(handle);
        return _arrays[handle];
    }

    /// <inheritdoc />
    public int GetArrayLength(int handle)
    {
        return GetArray(handle).Length;
    }

    /// <inheritdoc />
    public long GetElement(int handle, int index)
    {
        long[] array = GetArray(handle);
        ValidateIndex(array, index);
        return array[index];
    }

    /// <inheritdoc />
    public void SetElement(int handle, int index, long value)
    {
        long[] array = GetArray(handle);
        ValidateIndex(array, index);
        array[index] = value;
    }

    /// <inheritdoc />
    public void Collect(MemoryManager memory)
    {
        _ = memory; // намеренно ничего не делаем
    }

    /// <inheritdoc />
    public void Clear()
    {
        // намеренно не очищаем, чтобы сохранить утечки в режиме без GC
    }

    /// <inheritdoc />
    public Dictionary<long, long[]> GetAllArrays()
    {
        var result = new Dictionary<long, long[]>();
        for (int i = 0; i < _arrays.Count; i++)
        {
            result[Value.TagAsArray(i)] = _arrays[i];
        }

        return result;
    }

    private static void ValidateIndex(long[] array, int index)
    {
        if (index < 0 || index >= array.Length)
        {
            throw new IndexOutOfRangeException(
                $"Array index {index} out of bounds for array of length {array.Length}");
        }
    }

    private void ValidateHandle(int handle)
    {
        if (handle < 0 || handle >= _arrays.Count)
        {
            throw new IndexOutOfRangeException($"Invalid array handle: {handle}");
        }
    }
}
