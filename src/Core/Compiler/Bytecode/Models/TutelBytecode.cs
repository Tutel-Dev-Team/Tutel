using System.Collections.ObjectModel;

namespace Tutel.Core.Compiler.Bytecode.Models;

public class TutelBytecode
{
    public ushort EntryFunctionIndex { get; set; }

    public Collection<FunctionCode> Functions { get; } = [];

    public Collection<long> Globals { get; } = [];
}