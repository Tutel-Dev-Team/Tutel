// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Represents a value that can be stored on the operand stack or in variables.
/// In the Tutel VM, all values are 64-bit signed integers.
/// Array handles are stored as int indices into the heap.
/// </summary>
/// <remarks>
/// The VM uses a uniform representation where:
/// - Numeric values are stored directly as long
/// - Array handles are stored as long (cast from int handle index)
/// - Boolean results (from comparisons) are 1 for true, 0 for false.
/// </remarks>
public readonly record struct Value(long Raw)
{
    /// <summary>
    /// Creates a Value from an integer.
    /// </summary>
    public static implicit operator Value(long value) => new(value);

    /// <summary>
    /// Extracts the raw long value.
    /// </summary>
    public static implicit operator long(Value value) => value.Raw;

    /// <summary>
    /// Creates a Value from an array handle.
    /// </summary>
    public static Value FromArrayHandle(int handle) => new(handle);

    /// <summary>
    /// Extracts the array handle from this value.
    /// </summary>
    public int AsArrayHandle() => (int)Raw;

    /// <summary>
    /// Gets a value indicating whether this value is considered true (non-zero).
    /// </summary>
    public bool IsTrue => Raw != 0;

    /// <inheritdoc/>
    public override string ToString() => Raw.ToString();
}
