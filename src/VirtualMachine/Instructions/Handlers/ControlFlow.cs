// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;
using Tutel.VirtualMachine.Memory;

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for control flow instructions.
/// </summary>
public static class ControlFlow
{
    /// <summary>
    /// Executes JMP: PC += offset (unconditional jump).
    /// Returns the new PC instead of advancing normally.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True if PC was modified (jump taken).</returns>
    public static bool Jmp(ExecutionContext context, in DecodedInstruction instruction)
    {
        // Offset is relative to the start of the instruction
        context.ProgramCounter += instruction.Int32Arg;
        return true; // Indicate PC was modified
    }

    /// <summary>
    /// Executes JZ: Pop A, if A==0: PC += offset.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True if PC was modified (jump taken).</returns>
    public static bool Jz(ExecutionContext context, in DecodedInstruction instruction)
    {
        long value = context.Memory.OperandStack.Pop();
        if (value == 0)
        {
            // Jump relative to start of instruction
            context.ProgramCounter += instruction.Int32Arg;
            return true;
        }

        return false; // Normal PC advancement
    }

    /// <summary>
    /// Executes JNZ: Pop A, if A!=0: PC += offset.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True if PC was modified (jump taken).</returns>
    public static bool Jnz(ExecutionContext context, in DecodedInstruction instruction)
    {
        long value = context.Memory.OperandStack.Pop();
        if (value != 0)
        {
            // Jump relative to start of instruction
            context.ProgramCounter += instruction.Int32Arg;
            return true;
        }

        return false; // Normal PC advancement
    }

    /// <summary>
    /// Executes CALL: Push current frame, jump to function.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True (PC is always modified by function switch).</returns>
    public static bool Call(ExecutionContext context, in DecodedInstruction instruction)
    {
        ushort funcIndex = instruction.UInt16Arg;
        FunctionInfo targetFunc = context.GetFunction(funcIndex);

        // Calculate return address (after this CALL instruction)
        int returnAddress = context.ProgramCounter + instruction.Size;

        // Push new frame with return address
        context.Memory.CallStack.PushFrame(
            context.CurrentFunction.Index,
            returnAddress,
            targetFunc.LocalVariableCount);

        // Switch to target function (sets PC to 0)
        context.SwitchToFunction(funcIndex);

        return true; // PC is modified (set to 0 in new function)
    }

    /// <summary>
    /// Executes RET: Pop return value, pop frame, restore PC, push return value.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True (PC is always modified by return).</returns>
    public static bool Ret(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused

        OperandStack stack = context.Memory.OperandStack;
        CallStack callStack = context.Memory.CallStack;

        // Pop return value from stack
        long returnValue = stack.Pop();

        // Pop the current frame to get return info
        StackFrame frame = callStack.PopFrame();

        // Restore the calling function
        context.SwitchToFunction(frame.FunctionIndex);
        context.ProgramCounter = frame.ReturnAddress;

        // Push return value back onto stack
        stack.Push(returnValue);

        return true; // PC is modified (restored from frame)
    }
}
