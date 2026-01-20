// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Bytecode format constants.
/// </summary>
public static class BytecodeFormat
{
    /// <summary>
    /// Gets the magic number identifying a .tbc file: "MBCL" (0x4C42434D in little-endian).
    /// </summary>
    public static uint MagicNumber => 0x4C42434D;

    /// <summary>
    /// Gets the current bytecode version.
    /// </summary>
    public static ushort Version => 0x0001;
}
