// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Memory;

/// <summary>
/// Represents a value that can be stored on the operand stack or in variables.
/// In the Tutel VM, all values are stored as raw 64-bit payloads.
/// - Integers are stored directly as int64.
/// - Doubles are stored as their IEEE-754 bit pattern.
/// - Array handles are NaN-boxed to avoid collisions with double bit patterns.
/// </summary>
/// <remarks>
/// We previously used the high bit as a tag for arrays, which breaks as soon as
/// negative doubles appear (their high bit is also set). To support real double
/// arithmetic, we NaN-box array handles using a quiet-NaN pattern.
/// Boolean results (from comparisons) are still 1 for true, 0 for false.
/// </remarks>
public readonly record struct Value(long Raw)
{
    /// <summary>
    /// NaN-box tag mask and pattern used to distinguish array handles.
    /// The pattern corresponds to a quiet NaN with a reserved payload prefix.
    /// </summary>
    private const ulong ArrayTagMask = 0x7FF8_0000_0000_0000UL;
    private const ulong ArrayTagPattern = 0x7FF8_0000_0000_0000UL;
    private const ulong ArrayPayloadMask = 0x0000_0000_FFFF_FFFFUL;

    /// <summary>
    /// Creates a Value from an integer.
    /// </summary>
    public static implicit operator Value(long value) => new(value);

    /// <summary>
    /// Extracts the raw long value.
    /// </summary>
    public static implicit operator long(Value value) => value.Raw;

    /// <summary>
    /// Creates a Value from a double by converting it to long bits.
    /// </summary>
    public static Value FromDouble(double value) => new(BitConverter.DoubleToInt64Bits(value));

    /// <summary>
    /// Extracts the double value from this Value.
    /// </summary>
    public double AsDouble() => BitConverter.Int64BitsToDouble(Raw);

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
    /// Tags a handle as an array handle using NaN-boxing.
    /// </summary>
    internal static long TagAsArray(int handle)
    {
        if (handle < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), handle, "Array handle cannot be negative");
        }

        ulong payload = (uint)handle;
        return unchecked((long)(ArrayTagPattern | payload));
    }

    /// <summary>
    /// Checks if a raw value is an array handle.
    /// </summary>
    internal static bool IsArray(long value)
    {
        return ((ulong)value & ArrayTagMask) == ArrayTagPattern;
    }

    /// <summary>
    /// Extracts the handle from a tagged value.
    /// </summary>
    internal static int GetHandle(long value)
    {
        return (int)((ulong)value & ArrayPayloadMask);
    }

    /// <inheritdoc/>
    public override string ToString() => IsArrayHandle ? $"[H:{AsArrayHandle()}]" : Raw.ToString();
}
