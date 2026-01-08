// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Call stack managing function frames and return addresses.
/// </summary>
public sealed class CallStack
{
    private readonly Stack<StackFrame> _frames;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallStack"/> class.
    /// </summary>
    public CallStack()
    {
        _frames = new Stack<StackFrame>();
    }

    /// <summary>
    /// Gets the current call depth.
    /// </summary>
    public int Depth => _frames.Count;

    /// <summary>
    /// Gets a value indicating whether the call stack is empty.
    /// </summary>
    public bool IsEmpty => _frames.Count == 0;

    /// <summary>
    /// Gets the current (topmost) stack frame.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when no frames on stack.</exception>
    public StackFrame CurrentFrame
    {
        get
        {
            if (_frames.Count == 0)
            {
                throw new System.InvalidOperationException("No active stack frame");
            }

            return _frames.Peek();
        }
    }

    /// <summary>
    /// Pushes a new stack frame for a function call.
    /// </summary>
    /// <param name="functionIndex">The function being called.</param>
    /// <param name="returnAddress">PC to return to after function completes.</param>
    /// <param name="localVariableCount">Number of local variables.</param>
    /// <exception cref="System.InvalidOperationException">Thrown on stack overflow.</exception>
    public void PushFrame(ushort functionIndex, int returnAddress, int localVariableCount)
    {
        if (_frames.Count >= VmLimits.MaxCallStackDepth)
        {
            throw new System.InvalidOperationException(
                $"Call stack overflow: maximum depth of {VmLimits.MaxCallStackDepth} exceeded");
        }

        var frame = new StackFrame(functionIndex, returnAddress, localVariableCount);
        _frames.Push(frame);
    }

    /// <summary>
    /// Pops the current stack frame on function return.
    /// </summary>
    /// <returns>The popped frame containing the return address.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when no frames on stack.</exception>
    public StackFrame PopFrame()
    {
        if (_frames.Count == 0)
        {
            throw new System.InvalidOperationException("Call stack underflow: no frame to pop");
        }

        return _frames.Pop();
    }

    /// <summary>
    /// Clears all frames from the call stack.
    /// </summary>
    public void Clear()
    {
        _frames.Clear();
    }
}
