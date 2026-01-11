// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Represents a value that can be stored on the operand stack or in variables.
/// In the Tutel VM, all values are 64-bit signed integers.
/// Array handles are stored as int indices into the heap with a tag bit.
/// </summary>
/// <remarks>
/// The VM uses a tagged representation where:
/// - Numeric values are stored directly as long (high bit = 0)
/// - Array handles are stored as long with high bit set (high bit = 1)
/// - Boolean results (from comparisons) are 1 for true, 0 for false.
/// </remarks>
public readonly record struct Value(long Raw)
{
    /// <summary>
    /// Tag bit used to distinguish array handles from numeric values.
    /// Set to 1 for array handles, 0 for numbers.
    /// </summary>
    private const long ArrayTag = 1L << 63;

    /// <summary>
    /// Creates a Value from an integer.
    /// </summary>
    public static implicit operator Value(long value) => new(value);

    /// <summary>
    /// Extracts the raw long value.
    /// </summary>
    public static implicit operator long(Value value) => value.Raw;

    /// <summary>
    /// Creates a Value from an array handle with tag.
    /// </summary>
    public static Value FromArrayHandle(int handle) => new(TagAsArray(handle));

    /// <summary>
    /// Extracts the array handle from this value (removes tag).
    /// </summary>
    public int AsArrayHandle() => GetHandle(Raw);

    /// <summary>
    /// Gets a value indicating whether this value represents an array handle.
    /// </summary>
    public bool IsArrayHandle => IsArray(Raw);

    /// <summary>
    /// Gets a value indicating whether this value is considered true (non-zero).
    /// </summary>
    public bool IsTrue => Raw != 0;

    /// <summary>
    /// Tags a handle as an array handle by setting the high bit.
    /// </summary>
#pragma warning disable IDE0004 // Remove unnecessary cast
    internal static long TagAsArray(int handle) => unchecked((long)((ulong)(uint)handle | (ulong)ArrayTag));
#pragma warning restore IDE0004

    /// <summary>
    /// Checks if a raw value is an array handle.
    /// </summary>
    internal static bool IsArray(long value) => (value & ArrayTag) != 0;

    /// <summary>
    /// Extracts the handle from a tagged value.
    /// </summary>
    internal static int GetHandle(long value) => (int)(value & ~ArrayTag);

    /// <inheritdoc/>
    public override string ToString() => IsArrayHandle ? $"[H:{AsArrayHandle()}]" : Raw.ToString();
}
