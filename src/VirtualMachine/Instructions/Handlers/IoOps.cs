// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for I/O instructions.
/// </summary>
public static class IoOps
{
    /// <summary>
    /// Executes PRINT_INT: Pop value and print it to stdout.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void PrintInt(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        long value = context.Memory.OperandStack.Pop();
        Console.WriteLine(value);
    }

    /// <summary>
    /// Executes READ_INT: Read integer from stdin and push onto stack.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void ReadInt(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        string? input = Console.ReadLine();
        if (long.TryParse(input, out long value))
        {
            context.Memory.OperandStack.Push(value);
        }
        else
        {
            context.Memory.OperandStack.Push(0L);
        }
    }
}
