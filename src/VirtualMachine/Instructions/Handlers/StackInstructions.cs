// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for stack manipulation instructions.
/// </summary>
public static class StackInstructions
{
    /// <summary>
    /// Executes PUSH_INT: pushes an int64 value onto the operand stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void PushInt(ExecutionContext context, in DecodedInstruction instruction)
    {
        context.Memory.OperandStack.Push(instruction.Int64Arg);
    }

    /// <summary>
    /// Executes POP: pops and discards the top value from the operand stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Pop(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        context.Memory.OperandStack.Pop();
    }

    /// <summary>
    /// Executes DUP: duplicates the top value on the operand stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Dup(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        context.Memory.OperandStack.DuplicateTop();
    }

    public static void I2D(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        long a = context.Memory.OperandStack.Pop();
        context.Memory.OperandStack.Push(Memory.Value.FromDouble(a).Raw);
    }
}
