// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Instructions.Handlers;

/// <summary>
/// Handlers for arithmetic instructions.
/// </summary>
public static class ArithmeticOps
{
    /// <summary>
    /// Executes ADD: Pop B, Pop A, Push A+B.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Add(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        Memory.OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a + b);
    }

    /// <summary>
    /// Executes SUB: Pop B, Pop A, Push A-B.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Sub(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        Memory.OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a - b);
    }

    /// <summary>
    /// Executes MUL: Pop B, Pop A, Push A*B.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Mul(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        Memory.OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();
        stack.Push(a * b);
    }

    /// <summary>
    /// Executes DIV: Pop B, Pop A, Push A/B. Throws on division by zero.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <exception cref="System.DivideByZeroException">Thrown when B is zero.</exception>
    public static void Div(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        Memory.OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();

        if (b == 0)
        {
            throw new DivideByZeroException("Division by zero");
        }

        stack.Push(a / b);
    }

    /// <summary>
    /// Executes MOD: Pop B, Pop A, Push A%B. Throws on division by zero.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    /// <exception cref="System.DivideByZeroException">Thrown when B is zero.</exception>
    public static void Mod(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        Memory.OperandStack stack = context.Memory.OperandStack;
        long b = stack.Pop();
        long a = stack.Pop();

        if (b == 0)
        {
            throw new DivideByZeroException("Modulo by zero");
        }

        stack.Push(a % b);
    }

    /// <summary>
    /// Executes NEG: Pop A, Push -A.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="instruction">Decoded instruction.</param>
    public static void Neg(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction; // Unused
        Memory.OperandStack stack = context.Memory.OperandStack;
        long a = stack.Pop();
        stack.Push(-a);
    }

    public static void DAdd(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double b = new Memory.Value(stack.Pop()).AsDouble();
        double a = new Memory.Value(stack.Pop()).AsDouble();
        stack.Push(Memory.Value.FromDouble(a + b).Raw);
    }

    public static void DSub(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double b = new Memory.Value(stack.Pop()).AsDouble();
        double a = new Memory.Value(stack.Pop()).AsDouble();
        stack.Push(Memory.Value.FromDouble(a - b).Raw);
    }

    public static void DMul(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double b = new Memory.Value(stack.Pop()).AsDouble();
        double a = new Memory.Value(stack.Pop()).AsDouble();
        stack.Push(Memory.Value.FromDouble(a * b).Raw);
    }

    public static void DDiv(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double b = new Memory.Value(stack.Pop()).AsDouble();
        double a = new Memory.Value(stack.Pop()).AsDouble();

        if (b == 0.0)
            throw new DivideByZeroException("Division by zero");

        stack.Push(Memory.Value.FromDouble(a / b).Raw);
    }

    public static void DMod(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double b = new Memory.Value(stack.Pop()).AsDouble();
        double a = new Memory.Value(stack.Pop()).AsDouble();

        if (b == 0.0)
            throw new DivideByZeroException("Modulo by zero");

        stack.Push(Memory.Value.FromDouble(a % b).Raw);
    }

    public static void DNeg(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double a = new Memory.Value(stack.Pop()).AsDouble();
        stack.Push(Memory.Value.FromDouble(-a).Raw);
    }

    public static void DSqrt(ExecutionContext context, in DecodedInstruction instruction)
    {
        _ = instruction;
        Memory.OperandStack stack = context.Memory.OperandStack;
        double a = new Memory.Value(stack.Pop()).AsDouble();
        double result = Math.Sqrt(a);
        stack.Push(Memory.Value.FromDouble(result).Raw);
    }
}
