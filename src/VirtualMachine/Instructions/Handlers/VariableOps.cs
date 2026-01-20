// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for variable access instructions.
/// </summary>
public static class VariableOps
{
    /// <summary>
    /// Executes LOAD_LOCAL: Push local variable value onto stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void LoadLocal(ExecutionContext context, in DecodedInstruction instruction)
    {
        byte index = instruction.ByteArg;
        long value = context.Memory.GetLocal(index);
        context.Memory.OperandStack.Push(value);
    }

    /// <summary>
    /// Executes STORE_LOCAL: Pop value and store in local variable.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void StoreLocal(ExecutionContext context, in DecodedInstruction instruction)
    {
        byte index = instruction.ByteArg;
        long value = context.Memory.OperandStack.Pop();
        context.Memory.SetLocal(index, value);
    }

    /// <summary>
    /// Executes LOAD_GLOBAL: Push global variable value onto stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void LoadGlobal(ExecutionContext context, in DecodedInstruction instruction)
    {
        ushort index = instruction.UInt16Arg;
        long value = context.Memory.GetGlobal(index);
        context.Memory.OperandStack.Push(value);
    }

    /// <summary>
    /// Executes STORE_GLOBAL: Pop value and store in global variable.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void StoreGlobal(ExecutionContext context, in DecodedInstruction instruction)
    {
        ushort index = instruction.UInt16Arg;
        long value = context.Memory.OperandStack.Pop();
        context.Memory.SetGlobal(index, value);
    }
}
