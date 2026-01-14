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
    /// Executes JMP: PC = PC + instruction_size + offset (unconditional jump).
    /// Offset is relative to the NEXT instruction per spec.
    /// Returns the new PC instead of advancing normally.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True if PC was modified (jump taken).</returns>
    public static bool Jmp(ExecutionContext context, in DecodedInstruction instruction)
    {
        // Offset is relative to the next instruction (after this JMP)
        context.ProgramCounter += instruction.Size + instruction.Int32Arg;
        return true; // Indicate PC was modified
    }

    /// <summary>
    /// Executes JZ: Pop A, if A==0: PC = PC + instruction_size + offset.
    /// Offset is relative to the NEXT instruction per spec.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True if PC was modified (jump taken).</returns>
    public static bool Jz(ExecutionContext context, in DecodedInstruction instruction)
    {
        long value = context.Memory.OperandStack.Pop();
        if (value == 0)
        {
            // Jump relative to next instruction
            context.ProgramCounter += instruction.Size + instruction.Int32Arg;
            return true;
        }

        return false; // Normal PC advancement
    }

    /// <summary>
    /// Executes JNZ: Pop A, if A!=0: PC = PC + instruction_size + offset.
    /// Offset is relative to the NEXT instruction per spec.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True if PC was modified (jump taken).</returns>
    public static bool Jnz(ExecutionContext context, in DecodedInstruction instruction)
    {
        long value = context.Memory.OperandStack.Pop();
        if (value != 0)
        {
            // Jump relative to next instruction
            context.ProgramCounter += instruction.Size + instruction.Int32Arg;
            return true;
        }

        return false; // Normal PC advancement
    }

    /// <summary>
    /// Executes CALL: Pop arguments, push new frame, jump to function.
    /// Arguments are popped from operand stack and stored in callee's locals 0..arity-1.
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

        // Pop arguments from operand stack (in reverse order, so we store correctly)
        // Arguments are pushed left-to-right, so we pop right-to-left
        byte arity = targetFunc.Arity;
        long[] args = new long[arity];
        for (int i = arity - 1; i >= 0; i--)
        {
            args[i] = context.Memory.OperandStack.Pop();
        }

        // Push new frame with return address
        context.Memory.CallStack.PushFrame(
            context.CurrentFunction.Index,
            returnAddress,
            targetFunc.LocalVariableCount);

        // Store arguments in the new frame's local variables
        for (int i = 0; i < arity; i++)
        {
            context.Memory.CallStack.CurrentFrame.SetLocal((byte)i, args[i]);
        }

        // Switch to target function (sets PC to 0)
        context.SwitchToFunction(funcIndex);

        return true; // PC is modified (set to 0 in new function)
    }

    /// <summary>
    /// Executes RET: Pop return value (or 0 if stack empty), pop frame, restore PC, push return value.
    /// If returning from entry function (returnAddress == -1), halt execution.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <returns>True (PC is always modified by return).</returns>
    public static bool Ret(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused

        OperandStack stack = context.Memory.OperandStack;
        CallStack callStack = context.Memory.CallStack;

        // Pop return value from stack (use 0 if stack is empty - void function)
        long returnValue = stack.IsEmpty ? 0 : stack.Pop();

        // Pop the current frame to get return info
        StackFrame frame = callStack.PopFrame();

        // Check if this is return from entry function (sentinel return address)
        if (frame.ReturnAddress < 0)
        {
            // Returning from entry function - halt execution with return value
            context.Result = returnValue;
            context.Halted = true;
            return true;
        }

        // Restore the calling function
        context.SwitchToFunction(frame.FunctionIndex);
        context.ProgramCounter = frame.ReturnAddress;

        // Push return value back onto stack
        stack.Push(returnValue);

        return true; // PC is modified (restored from frame)
    }
}
