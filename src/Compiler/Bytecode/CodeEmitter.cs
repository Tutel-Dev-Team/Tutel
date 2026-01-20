using System.Collections.ObjectModel;
using Tutel.Core.Compiler.Bytecode.Enums;

namespace Tutel.Compiler.Bytecode;

public class CodeEmitter
{
    public Collection<byte> Code { get; } = [];

    private readonly Dictionary<string, int> _labels = new();
    private readonly List<(int Position, string Label)> _fixups = [];

    public int Position => Code.Count;

    public void Emit(OpCode opcode) => Code.Add((byte)opcode);

    public void EmitByte(byte value) => Code.Add(value);

    public void EmitInt32(int value)
    {
        Code.Add((byte)(value & 0xFF));
        Code.Add((byte)((value >> 8) & 0xFF));
        Code.Add((byte)((value >> 16) & 0xFF));
        Code.Add((byte)((value >> 24) & 0xFF));
    }

    public void EmitInt64(long value)
    {
        for (int i = 0; i < 8; i++)
        {
            Code.Add((byte)((value >> (i * 8)) & 0xFF));
        }
    }

    public void EmitUInt16(ushort value)
    {
        Code.Add((byte)(value & 0xFF));
        Code.Add((byte)((value >> 8) & 0xFF));
    }

    public void DefineLabel(string label) => _labels[label] = Position;

    public void EmitJump(OpCode opcode, string label)
    {
        Emit(opcode);
        _fixups.Add((Position, label));
        EmitInt32(0);
    }

    public void ResolveFixups()
    {
        foreach ((int position, string label) in _fixups)
        {
            if (!_labels.TryGetValue(label, out int target))
                throw new InvalidOperationException($"Метка '{label}' не определена");

            int nextInstructionPosition = position + 4;
            int offset = target - nextInstructionPosition;

            Code[position] = (byte)(offset & 0xFF);
            Code[position + 1] = (byte)((offset >> 8) & 0xFF);
            Code[position + 2] = (byte)((offset >> 16) & 0xFF);
            Code[position + 3] = (byte)((offset >> 24) & 0xFF);
        }
    }

    public void Clear()
    {
        Code.Clear();
        _labels.Clear();
        _fixups.Clear();
    }
}