// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for miscellaneous instructions.
/// </summary>
public static class MiscOps
{
    /// <summary>
    /// Executes NOP: No operation.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Nop(ExecutionContext context, in DecodedInstruction instruction)
    {
        // Explicitly mark parameters as unused to satisfy analyzers
        _ = context;
        _ = instruction;

        // No operation
    }

    /// <summary>
    /// Executes HALT: Stop execution, set result to top of stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Halt(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused

        // If stack is not empty, the result is the top value
        if (!context.Memory.OperandStack.IsEmpty)
        {
            context.Result = context.Memory.OperandStack.Pop();
        }

        context.Halted = true;
    }
}
