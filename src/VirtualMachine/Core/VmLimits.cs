// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// VM execution limits.
/// </summary>
public static class VmLimits
{
    /// <summary>
    /// Gets the maximum operand stack size.
    /// </summary>
    public static int MaxOperandStackSize => 65536;

    /// <summary>
    /// Gets the maximum call stack depth.
    /// </summary>
    public static int MaxCallStackDepth => 1024;

    /// <summary>
    /// Gets the maximum number of local variables per function.
    /// </summary>
    public static int MaxLocalVariables => 256;

    /// <summary>
    /// Gets the maximum number of global variables.
    /// </summary>
    public static int MaxGlobalVariables => 65536;
}
