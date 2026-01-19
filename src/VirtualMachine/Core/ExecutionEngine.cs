// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Tutel.VirtualMachine.Instructions.Handlers;

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Main execution engine that runs the bytecode interpretation loop.
/// </summary>
public sealed class ExecutionEngine
{
    /// <summary>
    /// Executes the bytecode module from its entry point.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <param name="trace">Enable instruction tracing output.</param>
    /// <param name="traceLimit">Maximum instructions to trace (0 = unlimited).</param>
    /// <returns>The execution result.</returns>
    public long Execute(Instructions.ExecutionContext context, bool trace = false, int traceLimit = 100)
    {
        ArgumentNullException.ThrowIfNull(context);

        int traceCount = 0;
        bool traceLimitReached = false;

        while (!context.Halted && context.ProgramCounter >= 0)
        {
            if (trace && (traceLimit == 0 || traceCount < traceLimit))
            {
                TraceInstruction(context, ref traceCount, traceLimit, ref traceLimitReached);
            }

            ExecuteNextInstruction(context);
        }

        return context.Result;
    }

    private static void TraceInstruction(
        Instructions.ExecutionContext context,
        ref int traceCount,
        int traceLimit,
        ref bool traceLimitReached)
    {
        int pc = context.ProgramCounter;
        Instructions.DecodedInstruction instr = Instructions.InstructionDecoder.Decode(context.Bytecode, pc);
        int stackSize = context.Memory.OperandStack.Count;
        ushort funcIdx = context.CurrentFunction.Index;

        Console.Error.WriteLine($"[{traceCount,4}] func={funcIdx} PC={pc,3} {instr.Opcode,-12} stack={stackSize}");
        traceCount++;

        if (traceLimit > 0 && traceCount >= traceLimit && !traceLimitReached)
        {
            Console.Error.WriteLine($"... (trace limit {traceLimit} reached, use --trace=0 for unlimited)");
            traceLimitReached = true;
        }
    }

    private static void ExecuteNextInstruction(Instructions.ExecutionContext context)
    {
        int pc = context.ProgramCounter;

        // Decode instruction
        Instructions.DecodedInstruction instruction = Instructions.InstructionDecoder.Decode(context.Bytecode, pc);

        // Execute based on opcode and determine if PC was modified
        bool pcModified = ExecuteInstruction(context, instruction);

        // Advance PC if not modified by the instruction
        if (!pcModified)
        {
            context.ProgramCounter += instruction.Size;
        }
    }

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Switch on opcode requires many cases")]
    private static bool ExecuteInstruction(Instructions.ExecutionContext context, in Instructions.DecodedInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            // Stack operations
            case Opcode.PushInt:
                StackInstructions.PushInt(context, in instruction);
                return false;
            case Opcode.Pop:
                StackInstructions.Pop(context, in instruction);
                return false;
            case Opcode.Dup:
                StackInstructions.Dup(context, in instruction);
                return false;

            // Arithmetic operations
            case Opcode.Add:
                ArithmeticOps.Add(context, in instruction);
                return false;
            case Opcode.Sub:
                ArithmeticOps.Sub(context, in instruction);
                return false;
            case Opcode.Mul:
                ArithmeticOps.Mul(context, in instruction);
                return false;
            case Opcode.Div:
                ArithmeticOps.Div(context, in instruction);
                return false;
            case Opcode.Mod:
                ArithmeticOps.Mod(context, in instruction);
                return false;
            case Opcode.Neg:
                ArithmeticOps.Neg(context, in instruction);
                return false;

            // Comparison operations
            case Opcode.CmpEq:
                ComparisonOps.CmpEq(context, in instruction);
                return false;
            case Opcode.CmpNe:
                ComparisonOps.CmpNe(context, in instruction);
                return false;
            case Opcode.CmpLt:
                ComparisonOps.CmpLt(context, in instruction);
                return false;
            case Opcode.CmpLe:
                ComparisonOps.CmpLe(context, in instruction);
                return false;
            case Opcode.CmpGt:
                ComparisonOps.CmpGt(context, in instruction);
                return false;
            case Opcode.CmpGe:
                ComparisonOps.CmpGe(context, in instruction);
                return false;

            // Control flow - these modify PC
            case Opcode.Jmp:
                return ControlFlow.Jmp(context, in instruction);
            case Opcode.Jz:
                return ControlFlow.Jz(context, in instruction);
            case Opcode.Jnz:
                return ControlFlow.Jnz(context, in instruction);
            case Opcode.Call:
                return ControlFlow.Call(context, in instruction);
            case Opcode.Ret:
                return ControlFlow.Ret(context, in instruction);

            // Variables
            case Opcode.LoadLocal:
                VariableOps.LoadLocal(context, in instruction);
                return false;
            case Opcode.StoreLocal:
                VariableOps.StoreLocal(context, in instruction);
                return false;
            case Opcode.LoadGlobal:
                VariableOps.LoadGlobal(context, in instruction);
                return false;
            case Opcode.StoreGlobal:
                VariableOps.StoreGlobal(context, in instruction);
                return false;

            // Arrays
            case Opcode.ArrayNew:
                ArrayOps.ArrayNew(context, in instruction);
                return false;
            case Opcode.ArrayLoad:
                ArrayOps.ArrayLoad(context, in instruction);
                return false;
            case Opcode.ArrayStore:
                ArrayOps.ArrayStore(context, in instruction);
                return false;
            case Opcode.ArrayLen:
                ArrayOps.ArrayLen(context, in instruction);
                return false;

            // Misc
            case Opcode.Nop:
                MiscOps.Nop(context, in instruction);
                return false;
            case Opcode.Halt:
                MiscOps.Halt(context, in instruction);
                return false;

            default:
                throw new InvalidOperationException(
                    $"Unhandled opcode: {instruction.Opcode} (0x{(byte)instruction.Opcode:X2}) at PC={context.ProgramCounter}");
        }
    }
}