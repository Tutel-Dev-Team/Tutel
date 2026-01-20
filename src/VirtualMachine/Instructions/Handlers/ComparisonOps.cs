// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Memory;

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for comparison instructions.
/// </summary>
public static class ComparisonOps
{
    /// <summary>
    /// Executes CMP_EQ: Pop B, Pop A, Push (A==B ? 1 : 0).
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void CmpEq(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a == b ? 1L : 0L);
    }

    /// <summary>
    /// Executes CMP_NE: Pop B, Pop A, Push (A!=B ? 1 : 0).
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void CmpNe(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a != b ? 1L : 0L);
    }

    /// <summary>
    /// Executes CMP_LT: Pop B, Pop A, Push (A&lt;B ? 1 : 0).
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void CmpLt(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a < b ? 1L : 0L);
    }

    /// <summary>
    /// Executes CMP_LE: Pop B, Pop A, Push (A&lt;=B ? 1 : 0).
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void CmpLe(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a <= b ? 1L : 0L);
    }

    /// <summary>
    /// Executes CMP_GT: Pop B, Pop A, Push (A&gt;B ? 1 : 0).
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void CmpGt(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a > b ? 1L : 0L);
    }

    /// <summary>
    /// Executes CMP_GE: Pop B, Pop A, Push (A&gt;=B ? 1 : 0).
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void CmpGe(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a >= b ? 1L : 0L);
    }
}
