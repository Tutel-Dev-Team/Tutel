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
    /// Executes ARRAY_NEW: Pop size, allocate array, push handle.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayNew(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        Heap heap = context.Memory.Heap;

        long size = stack.Pop();
        int handle = heap.AllocateArray((int)size);
        stack.Push(handle);
    }

    /// <summary>
    /// Executes ARRAY_LOAD: Pop index, pop handle, push array[index].
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayLoad(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        Heap heap = context.Memory.Heap;

        long index = stack.Pop();
        int handle = (int)stack.Pop();
        long value = heap.GetElement(handle, (int)index);
        stack.Push(value);
    }

    /// <summary>
    /// Executes ARRAY_STORE: Pop value, pop index, pop handle, array[index] = value.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayStore(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        Heap heap = context.Memory.Heap;

        long value = stack.Pop();
        long index = stack.Pop();
        int handle = (int)stack.Pop();
        heap.SetElement(handle, (int)index, value);
    }

    /// <summary>
    /// Executes ARRAY_LEN: Pop handle, push array length.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ArrayLen(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        Heap heap = context.Memory.Heap;

        int handle = (int)stack.Pop();
        int length = heap.GetArrayLength(handle);
        stack.Push(length);
    }
}
