// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Represents a loaded bytecode module containing functions and module metadata.
/// </summary>
public sealed class BytecodeModule
{
    private readonly Dictionary<ushort, FunctionInfo> _functions;

    /// <summary>
    /// Initializes a new instance of the <see cref="BytecodeModule"/> class.
    /// </summary>
    /// <param name="version">Bytecode version.</param>
    /// <param name="globalVariableCount">Number of global variables.</param>
    /// <param name="entryPointIndex">Entry point function index.</param>
    /// <param name="functions">Dictionary of functions by index.</param>
    public BytecodeModule(
        uint version,
        ushort globalVariableCount,
        uint entryPointIndex,
        Dictionary<ushort, FunctionInfo> functions)
    {
        ArgumentNullException.ThrowIfNull(functions);
        Version = version;
        GlobalVariableCount = globalVariableCount;
        EntryPointIndex = entryPointIndex;
        _functions = functions;
    }

    /// <summary>
    /// Gets the bytecode version.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Gets the number of global variables.
    /// </summary>
    public ushort GlobalVariableCount { get; }

    /// <summary>
    /// Gets the entry point function index.
    /// </summary>
    public uint EntryPointIndex { get; }

    /// <summary>
    /// Gets the number of functions in the module.
    /// </summary>
    public int FunctionCount => _functions.Count;

    /// <summary>
    /// Gets function information by index.
    /// </summary>
    /// <param name="index">The function index.</param>
    /// <returns>The function info.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when function index not found.</exception>
    public FunctionInfo GetFunction(ushort index)
    {
        if (!_functions.TryGetValue(index, out FunctionInfo? function))
        {
            throw new KeyNotFoundException($"Function with index {index} not found in module");
        }

        return function;
    }

    /// <summary>
    /// Gets the entry point function.
    /// </summary>
    /// <returns>The entry point function info.</returns>
    public FunctionInfo GetEntryPoint()
    {
        return GetFunction((ushort)EntryPointIndex);
    }

    /// <summary>
    /// Checks if a function with the given index exists.
    /// </summary>
    /// <param name="index">The function index.</param>
    /// <returns>True if the function exists.</returns>
    public bool HasFunction(ushort index)
    {
        return _functions.ContainsKey(index);
    }

    public IEnumerable<FunctionInfo> GetAllFunctions()
    {
        return _functions.Values;
    }
}
