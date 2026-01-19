using System.Collections.ObjectModel;
using Tutel.Core.Compiler.Bytecode.Models;

namespace Tutel.Compiler.Bytecode;

public class BytecodeWriter
{
    public void WriteToFile(TutelBytecode bytecode, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        using var writer = new BinaryWriter(stream);

        WriteHeader(writer, bytecode);
        WriteFunctionTable(writer, bytecode.Functions);
        WriteGlobalsSection(writer, bytecode.Globals);
    }

    private void WriteHeader(BinaryWriter writer, TutelBytecode bytecode)
    {
        writer.Write(new byte[] { 0x4D, 0x43, 0x42, 0x4C });
        writer.Write((ushort)1);
        writer.Write(bytecode.EntryFunctionIndex);
        writer.Write((ushort)bytecode.Globals.Count);
        writer.Write((ushort)bytecode.Functions.Count);
    }

    private void WriteFunctionTable(BinaryWriter writer, Collection<FunctionCode> functions)
    {
        foreach (FunctionCode function in functions)
        {
            writer.Write(function.Arity);
            writer.Write(function.LocalsCount);
            byte[] codeSizeBytes = BitConverter.GetBytes((uint)function.Code.Count);
            writer.Write(codeSizeBytes);
            writer.Write(function.Code.ToArray());
        }
    }

    private void WriteGlobalsSection(BinaryWriter writer, Collection<long> globals)
    {
        foreach (long globalValue in globals)
        {
            byte[] bytes = BitConverter.GetBytes(globalValue);
            writer.Write(bytes);
        }
    }
}