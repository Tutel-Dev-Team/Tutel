using System.Collections.ObjectModel;
using Tutel.Core.Compiler.SemanticAnalysis.Models;

namespace Tutel.Compiler.SemanticAnalysis;

public class SymbolTable
{
    public Collection<VariableSymbol> Globals { get; } = [];

    public Collection<FunctionSymbol> Functions { get; } = [];

    public Collection<SemanticError> Errors { get; } = [];

    public void AddGlobal(VariableSymbol symbol)
    {
        Globals.Add(symbol);
    }

    public void AddFunction(FunctionSymbol symbol)
    {
        Functions.Add(symbol);
    }

    public VariableSymbol? FindGlobal(string name)
    {
        return Globals.FirstOrDefault(g => g.Name == name);
    }

    public FunctionSymbol? FindFunction(string name)
    {
        return Functions.FirstOrDefault(f => f.Name == name);
    }
}