// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Memory;

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for array instructions.
/// </summary>
public static class ArrayOps
{
    /// <summary>
    /// Executes ARRAY_NEW: Pop size, allocate array, push tagged handle.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayNew(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;

        long size = stack.Pop();
        long taggedHandle = context.Memory.AllocateArray((int)size);
        stack.Push(taggedHandle);
    }

    /// <summary>
    /// Executes ARRAY_LOAD: Pop index, pop tagged handle, push array[index].
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayLoad(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        IGarbageCollector gc = context.Memory.GC;

        long index = stack.Pop();
        long taggedHandle = stack.Pop();
        if (!Value.IsArray(taggedHandle))
        {
            throw new InvalidOperationException($"Expected array handle, got {taggedHandle}");
        }

        int handle = Value.GetHandle(taggedHandle);
        long value = gc.GetElement(handle, (int)index);
        stack.Push(value);
    }

    /// <summary>
    /// Executes ARRAY_STORE: Pop value, pop index, pop tagged handle, array[index] = value.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayStore(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        IGarbageCollector gc = context.Memory.GC;

        long value = stack.Pop();
        long index = stack.Pop();
        long taggedHandle = stack.Pop();
        if (!Value.IsArray(taggedHandle))
        {
            throw new InvalidOperationException($"Expected array handle, got {taggedHandle}");
        }

        int handle = Value.GetHandle(taggedHandle);
        gc.SetElement(handle, (int)index, value);
    }

    /// <summary>
    /// Executes ARRAY_LEN: Pop tagged handle, push array length.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayLen(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        IGarbageCollector gc = context.Memory.GC;

        long taggedHandle = stack.Pop();
        if (!Value.IsArray(taggedHandle))
        {
            throw new InvalidOperationException($"Expected array handle, got {taggedHandle}");
        }

        int handle = Value.GetHandle(taggedHandle);
        int length = gc.GetArrayLength(handle);
        stack.Push(length);
    }
}
