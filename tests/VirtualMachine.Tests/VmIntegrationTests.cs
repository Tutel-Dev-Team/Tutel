// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;
using Xunit;

namespace Tutel.VirtualMachine.Tests;

/// <summary>
/// Integration tests for the Tutel VM.
/// </summary>
public class VmIntegrationTests
{
    /// <summary>
    /// Test: PUSH_INT 42, HALT → returns 42.
    /// </summary>
    [Fact]
    public void SimplePushAndHalt_Returns42()
    {
        // Bytecode: PUSH_INT 42, HALT
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01,                                           // PUSH_INT opcode
            42, 0, 0, 0, 0, 0, 0, 0,                        // int64 value: 42 (little-endian)
            0xFF,                                           // HALT opcode
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(42, result);
    }

    /// <summary>
    /// Test: PUSH_INT 10, PUSH_INT 5, ADD, HALT → returns 15.
    /// </summary>
    [Fact]
    public void AddTwoNumbers_Returns15()
    {
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01, 10, 0, 0, 0, 0, 0, 0, 0,   // PUSH_INT 10
            0x01, 5, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 5
            0x10,                             // ADD
            0xFF,                             // HALT
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(15, result);
    }

    /// <summary>
    /// Test: PUSH_INT 10, PUSH_INT 3, SUB, HALT → returns 7.
    /// </summary>
    [Fact]
    public void SubtractNumbers_Returns7()
    {
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01, 10, 0, 0, 0, 0, 0, 0, 0,   // PUSH_INT 10
            0x01, 3, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 3
            0x11,                             // SUB
            0xFF,                             // HALT
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(7, result);
    }

    /// <summary>
    /// Test: PUSH_INT 6, PUSH_INT 7, MUL, HALT → returns 42.
    /// </summary>
    [Fact]
    public void MultiplyNumbers_Returns42()
    {
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01, 6, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 6
            0x01, 7, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 7
            0x12,                             // MUL
            0xFF,                             // HALT
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(42, result);
    }

    /// <summary>
    /// Test: PUSH_INT 5, NEG, HALT → returns -5.
    /// </summary>
    [Fact]
    public void NegateNumber_ReturnsMinus5()
    {
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01, 5, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 5
            0x15,                             // NEG
            0xFF,                             // HALT
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(-5, result);
    }

    /// <summary>
    /// Test: PUSH_INT 5, PUSH_INT 5, CMP_EQ, HALT → returns 1 (true).
    /// </summary>
    [Fact]
    public void CompareEqual_ReturnsTrue()
    {
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01, 5, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 5
            0x01, 5, 0, 0, 0, 0, 0, 0, 0,    // PUSH_INT 5
            0x20,                             // CMP_EQ
            0xFF,                             // HALT
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(1, result);
    }

    /// <summary>
    /// Test: PUSH_INT 42, DUP, ADD, HALT → returns 84.
    /// </summary>
    [Fact]
    public void DuplicateAndAdd_Returns84()
    {
        byte[] bytecode = CreateBytecode(new byte[]
        {
            0x01, 42, 0, 0, 0, 0, 0, 0, 0,   // PUSH_INT 42
            0x03,                             // DUP
            0x10,                             // ADD
            0xFF,                             // HALT
        });

        long result = TutelVm.RunBytes(bytecode);

        Assert.Equal(84, result);
    }

    /// <summary>
    /// Creates a valid .tbc bytecode module with a single function.
    /// </summary>
    private static byte[] CreateBytecode(byte[] functionBytecode)
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);

        // Header
        bw.Write(0x4C42434Du); // Magic: "MBCL"
        bw.Write(0x00000001u); // Version: 1
        bw.Write((ushort)1);   // Function count: 1
        bw.Write((ushort)0);   // Global variable count: 0
        bw.Write(0u);          // Entry point: function 0

        // Function 0
        bw.Write((ushort)0);   // Function index: 0
        bw.Write((ushort)0);   // Local variable count: 0
        bw.Write((uint)functionBytecode.Length);
        bw.Write(functionBytecode);

        return ms.ToArray();
    }
}
