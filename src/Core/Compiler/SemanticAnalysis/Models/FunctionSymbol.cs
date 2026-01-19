using System.Collections.ObjectModel;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.SemanticAnalysis.Models;

public class FunctionSymbol
{
    public string Name { get; set; } = string.Empty;

    public TypeNode ReturnType { get; set; } = new VoidType();

    public int Index { get; set; }

    public Collection<VariableSymbol> Parameters { get; } = [];

    public Collection<VariableSymbol> Locals { get; } = [];

    public int LocalSlotCount => Parameters.Count + Locals.Count;
}