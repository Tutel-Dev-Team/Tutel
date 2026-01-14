// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License;

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Wrapper around IGarbageCollector to provide backward-compatible Heap interface.
/// </summary>
internal sealed class HeapWrapper : Heap
{
    private readonly IGarbageCollector _gc;

    public HeapWrapper(IGarbageCollector gc)
    {
        _gc = gc ?? throw new ArgumentNullException(nameof(gc));
    }

    public override int AllocateArray(int size)
    {
        long taggedHandle = _gc.Allocate(size);
        return Value.GetHandle(taggedHandle);
    }

    public override long[] GetArray(int handle)
    {
        return _gc.GetArray(handle);
    }

    public override int GetArrayLength(int handle)
    {
        return _gc.GetArrayLength(handle);
    }

    public override long GetElement(int handle, int index)
    {
        return _gc.GetElement(handle, index);
    }

    public override void SetElement(int handle, int index, long value)
    {
        _gc.SetElement(handle, index, value);
    }

    public override void Clear()
    {
        _gc.Clear();
    }

    public override int ArrayCount => _gc.ArrayCount;
}
