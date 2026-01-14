// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Tutel.VirtualMachine.Jit;

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Tutel Virtual Machine - loads and executes .tbc bytecode files.
/// </summary>
public sealed class TutelVm
{
    private BytecodeModule? _module;
    private Memory.MemoryManager? _memory;
    private IJitRuntime? _jit;

    /// <summary>
    /// Gets a value indicating whether a module is loaded.
    /// </summary>
    public bool IsLoaded => _module != null;

    /// <summary>
    /// Runs bytecode from a file in one step.
    /// </summary>
    /// <param name="filePath">Path to the .tbc file.</param>
    /// <returns>The execution result.</returns>
    public static long RunFile(string filePath)
    {
        TutelVm vm = new();
        vm.Load(filePath);
        return vm.Run();
    }

    /// <summary>
    /// Runs bytecode from bytes in one step.
    /// </summary>
    /// <param name="data">The bytecode data.</param>
    /// <returns>The execution result.</returns>
    public static long RunBytes(byte[] data)
    {
        TutelVm vm = new();
        vm.LoadFromBytes(data);
        return vm.Run();
    }

    /// <summary>
    /// Loads a bytecode module from a file.
    /// </summary>
    /// <param name="filePath">Path to the .tbc file.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when loading fails.</exception>
    public void Load(string filePath)
    {
        _module = BytecodeLoader.LoadFromFile(filePath);
        _memory = new Memory.MemoryManager(_module.GlobalVariableCount);

        _jit = new JitRuntime(_module);
    }

    /// <summary>
    /// Loads a bytecode module from a byte array.
    /// </summary>
    /// <param name="data">The bytecode data.</param>
    /// <exception cref="System.InvalidOperationException">Thrown when loading fails.</exception>
    public void LoadFromBytes(byte[] data)
    {
        _module = BytecodeLoader.LoadFromBytes(data);
        _memory = new Memory.MemoryManager(_module.GlobalVariableCount);

        _jit = new JitRuntime(_module);
    }

    /// <summary>
    /// Runs the loaded bytecode module.
    /// </summary>
    /// <returns>The execution result.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when no module is loaded.</exception>
    public long Run()
    {
        if (_module == null || _memory == null)
        {
            throw new InvalidOperationException("No bytecode module loaded. Call Load() first.");
        }

        // Reset memory state for a fresh run
        _memory.Reset();

        // Create execution context starting at entry point
        Instructions.ExecutionContext context = new(
            _module,
            _memory,
            _jit ?? throw new InvalidOperationException("JIT runtime not initialized"));

        // Create initial frame for entry point function
        FunctionInfo entryPoint = _module.GetEntryPoint();
        context.SwitchToFunction(entryPoint.Index);
        context.ProgramCounter = 0;

        // _memory.CallStack.PushFrame(
        //     entryPoint.Index,
        //     returnAddress: -1, // Sentinel: no return from main
        //     entryPoint.LocalVariableCount);

        // Run the execution engine
        ExecutionEngine engine = new();
        long result = engine.Execute(context);

#if DEBUG
        DumpJitStats(_module.GetAllFunctions());
#endif
        return result;
    }

    public void DumpJitStats(IEnumerable<FunctionInfo> functions)
    {
        Console.WriteLine("=== JIT statistics ===");

        foreach (FunctionInfo fn in functions)
        {
            if (fn.JitCompileCount == 0 && fn.JitExecutionCount == 0)
                continue;

            double seconds = (double)fn.JitTotalTicks / Stopwatch.Frequency;

            Console.WriteLine(
                $"fn {fn.Index}: compiled={fn.JitCompileCount}, " +
                $"executed={fn.JitExecutionCount}, " +
                $"ticks={fn.JitTotalTicks}, " +
                $"time={seconds}s");
        }
    }
}