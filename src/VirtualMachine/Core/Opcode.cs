// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// VM opcodes for bytecode instructions.
/// </summary>
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Opcodes are byte-sized by design")]
public enum Opcode : byte
{
    /// <summary>No operation.</summary>
    Nop = 0x00,

    /// <summary>Push int64 value onto stack. Format: 1 byte opcode + 8 bytes int64.</summary>
    PushInt = 0x01,

    /// <summary>Pop and discard top of stack.</summary>
    Pop = 0x02,

    /// <summary>Duplicate top of stack.</summary>
    Dup = 0x03,

    /// <summary>Pop B, Pop A, Push A+B.</summary>
    Add = 0x10,

    /// <summary>Pop B, Pop A, Push A-B.</summary>
    Sub = 0x11,

    /// <summary>Pop B, Pop A, Push A*B.</summary>
    Mul = 0x12,

    /// <summary>Pop B, Pop A, Push A/B (throws if B==0).</summary>
    Div = 0x13,

    /// <summary>Pop B, Pop A, Push A%B (throws if B==0).</summary>
    Mod = 0x14,

    /// <summary>Pop A, Push -A.</summary>
    Neg = 0x15,

    /// <summary>Pop B, Pop A, Push (A==B ? 1 : 0).</summary>
    CmpEq = 0x20,

    /// <summary>Pop B, Pop A, Push (A!=B ? 1 : 0).</summary>
    CmpNe = 0x21,

    /// <summary>Pop B, Pop A, Push (A&lt;B ? 1 : 0).</summary>
    CmpLt = 0x22,

    /// <summary>Pop B, Pop A, Push (A&lt;=B ? 1 : 0).</summary>
    CmpLe = 0x23,

    /// <summary>Pop B, Pop A, Push (A&gt;B ? 1 : 0).</summary>
    CmpGt = 0x24,

    /// <summary>Pop B, Pop A, Push (A&gt;=B ? 1 : 0).</summary>
    CmpGe = 0x25,

    /// <summary>Unconditional jump. Format: 1 byte opcode + 4 bytes int32 offset.</summary>
    Jmp = 0x30,

    /// <summary>Jump if zero. Pop A, if A==0: PC += offset. Format: 1 byte opcode + 4 bytes int32 offset.</summary>
    Jz = 0x31,

    /// <summary>Jump if not zero. Pop A, if A!=0: PC += offset. Format: 1 byte opcode + 4 bytes int32 offset.</summary>
    Jnz = 0x32,

    /// <summary>Call function. Format: 1 byte opcode + 2 bytes uint16 function index.</summary>
    Call = 0x33,

    /// <summary>Return from function. Pop return value, restore PC, push return value.</summary>
    Ret = 0x34,

    /// <summary>Load local variable. Format: 1 byte opcode + 1 byte uint8 index.</summary>
    LoadLocal = 0x40,

    /// <summary>Store local variable. Format: 1 byte opcode + 1 byte uint8 index.</summary>
    StoreLocal = 0x41,

    /// <summary>Load global variable. Format: 1 byte opcode + 2 bytes uint16 index.</summary>
    LoadGlobal = 0x50,

    /// <summary>Store global variable. Format: 1 byte opcode + 2 bytes uint16 index.</summary>
    StoreGlobal = 0x51,

    /// <summary>Create new array. Pop size, allocate array, push handle.</summary>
    ArrayNew = 0x60,

    /// <summary>Load array element. Pop index, pop handle, push array[index].</summary>
    ArrayLoad = 0x61,

    /// <summary>Store array element. Pop value, pop index, pop handle, array[index] = value.</summary>
    ArrayStore = 0x62,

    /// <summary>Get array length. Pop handle, push length.</summary>
    ArrayLen = 0x63,

    /// <summary>Halt execution. Return top of stack as result.</summary>
    Halt = 0xFF,
}
