// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// LIFO stack for operand values during bytecode execution.
/// </summary>
public sealed class OperandStack
{
    private readonly Stack<long> _stack;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperandStack"/> class.
    /// </summary>
    public OperandStack()
    {
        _stack = new Stack<long>();
    }

    /// <summary>
    /// Gets the current number of values on the stack.
    /// </summary>
    public int Count => _stack.Count;

    /// <summary>
    /// Gets a value indicating whether the stack is empty.
    /// </summary>
    public bool IsEmpty => _stack.Count == 0;

    /// <summary>
    /// Pushes a value onto the stack.
    /// </summary>
    /// <param name="value">The value to push.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when stack overflow occurs.</exception>
    public void Push(long value)
    {
        if (_stack.Count >= VmLimits.MaxOperandStackSize)
        {
            throw new InvalidOperationException(
                $"Stack overflow: cannot push, stack has reached maximum size of {VmLimits.MaxOperandStackSize}");
        }

        _stack.Push(value);
    }

    /// <summary>
    /// Pops a value from the stack.
    /// </summary>
    /// <returns>The popped value.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when attempting to pop from empty stack.</exception>
    public long Pop()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack underflow: cannot pop from empty stack");
        }

        return _stack.Pop();
    }

    /// <summary>
    /// Peeks at the top value without removing it.
    /// </summary>
    /// <returns>The top value.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when stack is empty.</exception>
    public long Peek()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack underflow: cannot peek at empty stack");
        }

        return _stack.Peek();
    }

    /// <summary>
    /// Duplicates the top value on the stack.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when stack is empty or overflow would occur.</exception>
    public void DuplicateTop()
    {
        Push(Peek());
    }

    /// <summary>
    /// Clears all values from the stack.
    /// </summary>
    public void Clear()
    {
        _stack.Clear();
    }

    /// <summary>
    /// Enumerates all values on the stack for garbage collection.
    /// </summary>
    /// <returns>An enumerable of all stack values.</returns>
    internal IEnumerable<long> EnumerateValues()
    {
        return _stack;
    }
}
