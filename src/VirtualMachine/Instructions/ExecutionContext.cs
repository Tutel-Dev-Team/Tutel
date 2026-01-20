// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;
using Tutel.VirtualMachine.Jit;
using Tutel.VirtualMachine.Memory;

namespace Tutel.VirtualMachine.Instructions;

/// <summary>
/// Execution context providing access to VM state during instruction execution.
/// </summary>
public sealed class ExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
    /// </summary>
    /// <param name="module">The bytecode module being executed.</param>
    /// <param name="memory">The memory manager.</param>
    /// <param name="jit">The jit runtime.</param>
    public ExecutionContext(BytecodeModule module, MemoryManager memory, IJitRuntime jit)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(memory);
        Module = module;
        Memory = memory;
        CurrentFunction = module.GetEntryPoint();
        ProgramCounter = 0;
        Halted = false;
        Result = 0;
        Jit = jit;
    }

    /// <summary>
    /// Gets the bytecode module.
    /// </summary>
    public BytecodeModule Module { get; }

    /// <summary>
    /// Gets the memory manager.
    /// </summary>
    public MemoryManager Memory { get; }

    /// <summary>
    /// Gets or sets the current program counter (byte offset in current function's bytecode).
    /// </summary>
    public int ProgramCounter { get; set; }

    /// <summary>
    /// Gets or sets the current function being executed.
    /// </summary>
    public FunctionInfo CurrentFunction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether execution has halted.
    /// </summary>
    public bool Halted { get; set; }

    /// <summary>
    /// Gets or sets the execution result (set on HALT).
    /// </summary>
    public long Result { get; set; }

    /// <summary>
    /// Gets the current bytecode array.
    /// </summary>
    public byte[] Bytecode => CurrentFunction.Bytecode;

    public IJitRuntime Jit { get; }

    /// <summary>
    /// Reads a byte at the current PC and advances PC.
    /// </summary>
    /// <returns>The byte value.</returns>
    public byte ReadByte()
    {
        EnsureBytesAvailable(1);
        return Bytecode[ProgramCounter++];
    }

    /// <summary>
    /// Reads a signed 32-bit integer at current PC (little-endian) and advances PC by 4.
    /// </summary>
    /// <returns>The int32 value.</returns>
    public int ReadInt32()
    {
        EnsureBytesAvailable(4);
        int value = BitConverter.ToInt32(Bytecode, ProgramCounter);
        ProgramCounter += 4;
        return value;
    }

    /// <summary>
    /// Reads a signed 64-bit integer at current PC (little-endian) and advances PC by 8.
    /// </summary>
    /// <returns>The int64 value.</returns>
    public long ReadInt64()
    {
        EnsureBytesAvailable(8);
        long value = BitConverter.ToInt64(Bytecode, ProgramCounter);
        ProgramCounter += 8;
        return value;
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer at current PC (little-endian) and advances PC by 2.
    /// </summary>
    /// <returns>The uint16 value.</returns>
    public ushort ReadUInt16()
    {
        EnsureBytesAvailable(2);
        ushort value = BitConverter.ToUInt16(Bytecode, ProgramCounter);
        ProgramCounter += 2;
        return value;
    }

    /// <summary>
    /// Gets a function by index from the module.
    /// </summary>
    /// <param name="index">The function index.</param>
    /// <returns>The function info.</returns>
    public FunctionInfo GetFunction(ushort index)
    {
        return Module.GetFunction(index);
    }

    /// <summary>
    /// Switches execution to a different function at PC 0.
    /// </summary>
    /// <param name="functionIndex">The function to switch to.</param>
    public void SwitchToFunction(ushort functionIndex)
    {
        CurrentFunction = GetFunction(functionIndex);
        ProgramCounter = 0;
    }

    private void EnsureBytesAvailable(int count)
    {
        if (ProgramCounter + count > Bytecode.Length)
        {
            throw new InvalidOperationException(
                $"Unexpected end of bytecode: PC={ProgramCounter}, need {count} bytes, have {Bytecode.Length - ProgramCounter}");
        }
    }
}