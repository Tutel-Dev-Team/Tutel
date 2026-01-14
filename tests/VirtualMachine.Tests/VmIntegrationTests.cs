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
    [Fact]
    public void SimplePushAndHalt()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void PopRemovesTopValue()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 99, 0, 0, 0, 0, 0, 0, 0, 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0x02, 0xFF });
        Assert.Equal(99, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void DupDuplicatesTopValue()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 21, 0, 0, 0, 0, 0, 0, 0, 0x03, 0x10, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void AddTwoNumbers()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 10, 0, 0, 0, 0, 0, 0, 0, 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x10, 0xFF });
        Assert.Equal(15, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void SubTwoNumbers()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 10, 0, 0, 0, 0, 0, 0, 0, 0x01, 3, 0, 0, 0, 0, 0, 0, 0, 0x11, 0xFF });
        Assert.Equal(7, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void MulTwoNumbers()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 6, 0, 0, 0, 0, 0, 0, 0, 0x01, 7, 0, 0, 0, 0, 0, 0, 0, 0x12, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void DivTwoNumbers()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 20, 0, 0, 0, 0, 0, 0, 0, 0x01, 4, 0, 0, 0, 0, 0, 0, 0, 0x13, 0xFF });
        Assert.Equal(5, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void ModTwoNumbers()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 17, 0, 0, 0, 0, 0, 0, 0, 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x14, 0xFF });
        Assert.Equal(2, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void NegNumber()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x15, 0xFF });
        Assert.Equal(-5, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void DivByZeroThrows()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 10, 0, 0, 0, 0, 0, 0, 0, 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0x13, 0xFF });
        Assert.Throws<DivideByZeroException>(() => TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void CmpEqEqual()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x20, 0xFF });
        Assert.Equal(1, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void CmpEqNotEqual()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x01, 3, 0, 0, 0, 0, 0, 0, 0, 0x20, 0xFF });
        Assert.Equal(0, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void CmpLtTrue()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 3, 0, 0, 0, 0, 0, 0, 0, 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x22, 0xFF });
        Assert.Equal(1, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void CmpGtTrue()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 7, 0, 0, 0, 0, 0, 0, 0, 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x24, 0xFF });
        Assert.Equal(1, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void JmpUnconditional()
    {
        // JMP offset is from NEXT instruction. Original wanted to skip 9 bytes (PUSH 99), so offset = 9
        byte[] bytecode = CreateBytecode(new byte[] { 0x30, 9, 0, 0, 0, 0x01, 99, 0, 0, 0, 0, 0, 0, 0, 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void JzJumpsOnZero()
    {
        // After PUSH 0 (9 bytes), JZ checks if zero and jumps. Offset from next = 9 bytes to skip PUSH 99
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0x31, 9, 0, 0, 0, 0x01, 99, 0, 0, 0, 0, 0, 0, 0, 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void JnzJumpsOnNonZero()
    {
        // After PUSH 1 (9 bytes), JNZ checks if non-zero and jumps. Offset from next = 9 bytes to skip PUSH 99
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 1, 0, 0, 0, 0, 0, 0, 0, 0x32, 9, 0, 0, 0, 0x01, 99, 0, 0, 0, 0, 0, 0, 0, 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void LocalVariables()
    {
        byte[] bytecode = CreateBytecodeWithLocals(2, new byte[] { 0x01, 10, 0, 0, 0, 0, 0, 0, 0, 0x41, 0, 0x01, 20, 0, 0, 0, 0, 0, 0, 0, 0x41, 1, 0x40, 0, 0x40, 1, 0x10, 0xFF });
        Assert.Equal(30, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void GlobalVariables()
    {
        byte[] bytecode = CreateBytecodeWithGlobals(2, new byte[] { 0x01, 100, 0, 0, 0, 0, 0, 0, 0, 0x51, 0, 0, 0x01, 200, 0, 0, 0, 0, 0, 0, 0, 0x51, 1, 0, 0x50, 0, 0, 0x50, 1, 0, 0x10, 0xFF });
        Assert.Equal(300, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void ArrayCreateAndLength()
    {
        byte[] bytecode = CreateBytecode(new byte[] { 0x01, 5, 0, 0, 0, 0, 0, 0, 0, 0x60, 0x63, 0xFF });
        Assert.Equal(5, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void ArrayStoreAndLoad()
    {
        byte[] bytecode = CreateBytecodeWithLocals(1, new byte[] { 0x01, 3, 0, 0, 0, 0, 0, 0, 0, 0x60, 0x41, 0, 0x40, 0, 0x01, 1, 0, 0, 0, 0, 0, 0, 0, 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0x62, 0x40, 0, 0x01, 1, 0, 0, 0, 0, 0, 0, 0, 0x61, 0xFF });
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void SimpleCall()
    {
        byte[] func0 = new byte[] { 0x33, 1, 0, 0xFF };
        byte[] func1 = new byte[] { 0x01, 42, 0, 0, 0, 0, 0, 0, 0, 0x34 };
        byte[] bytecode = CreateTwoFunctions(func0, 0, func1, 0);
        Assert.Equal(42, TutelVm.RunBytes(bytecode));
    }

    [Fact]
    public void CallWithArgs()
    {
        // func0: Push 10, Push 20, Call func1, Halt
        byte[] func0 = new byte[] { 0x01, 10, 0, 0, 0, 0, 0, 0, 0, 0x01, 20, 0, 0, 0, 0, 0, 0, 0, 0x33, 1, 0, 0xFF };

        // func1: CALL now transfers args to locals automatically.
        // Args pushed left-to-right: 10 (arg0), 20 (arg1) -> locals[0]=10, locals[1]=20
        // Load local 0 (10), Load local 1 (20), Add (30), Ret
        byte[] func1 = new byte[] { 0x40, 0, 0x40, 1, 0x10, 0x34 };
        byte[] bytecode = CreateTwoFunctions(func0, 0, func1, 2);
        Assert.Equal(30, TutelVm.RunBytes(bytecode));
    }

    private static byte[] CreateBytecode(byte[] code)
    {
        return CreateBytecodeWithLocals(0, code);
    }

    private static byte[] CreateBytecodeWithLocals(int locals, byte[] code)
    {
        MemoryStream ms = new();
        BinaryWriter bw = new(ms);
        bw.Write(0x4C42434Du);         // Magic "MBCL" (4 bytes)
        bw.Write((ushort)1);           // Version (2 bytes)
        bw.Write((ushort)0);           // Entry point index (2 bytes) - function 0
        bw.Write((ushort)0);           // Global variable count (2 bytes)
        bw.Write((ushort)1);           // Function count (2 bytes)

        // Function 0:
        bw.Write((byte)0);             // Arity (1 byte) - no parameters
        bw.Write((byte)locals);        // Locals (1 byte)
        bw.Write((uint)code.Length);   // Code size (4 bytes)
        bw.Write(code);                // Code
        return ms.ToArray();
    }

    private static byte[] CreateBytecodeWithGlobals(int globals, byte[] code)
    {
        MemoryStream ms = new();
        BinaryWriter bw = new(ms);
        bw.Write(0x4C42434Du);         // Magic "MBCL" (4 bytes)
        bw.Write((ushort)1);           // Version (2 bytes)
        bw.Write((ushort)0);           // Entry point index (2 bytes) - function 0
        bw.Write((ushort)globals);     // Global variable count (2 bytes)
        bw.Write((ushort)1);           // Function count (2 bytes)

        // Function 0:
        bw.Write((byte)0);             // Arity (1 byte) - no parameters
        bw.Write((byte)0);             // Locals (1 byte)
        bw.Write((uint)code.Length);   // Code size (4 bytes)
        bw.Write(code);                // Code
        return ms.ToArray();
    }

    private static byte[] CreateTwoFunctions(byte[] func0Code, int func0Locals, byte[] func1Code, int func1Locals)
    {
        MemoryStream ms = new();
        BinaryWriter bw = new(ms);
        bw.Write(0x4C42434Du);         // Magic "MBCL" (4 bytes)
        bw.Write((ushort)1);           // Version (2 bytes)
        bw.Write((ushort)0);           // Entry point index (2 bytes) - function 0
        bw.Write((ushort)0);           // Global variable count (2 bytes)
        bw.Write((ushort)2);           // Function count (2 bytes)

        // Function 0:
        bw.Write((byte)0);             // Arity (1 byte)
        bw.Write((byte)func0Locals);   // Locals (1 byte)
        bw.Write((uint)func0Code.Length); // Code size (4 bytes)
        bw.Write(func0Code);           // Code

        // Function 1:
        bw.Write((byte)func1Locals);   // Arity (1 byte) - treat func1Locals as arity for arg passing
        bw.Write((byte)func1Locals);   // Locals (1 byte)
        bw.Write((uint)func1Code.Length); // Code size (4 bytes)
        bw.Write(func1Code);           // Code
        return ms.ToArray();
    }
}