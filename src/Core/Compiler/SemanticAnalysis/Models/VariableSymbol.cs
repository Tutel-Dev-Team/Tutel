using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.SemanticAnalysis.Models;

public class VariableSymbol
{
    public VariableSymbol(string name, TypeNode type, int index)
    {
        Name = name;
        Type = type;
        Index = index;
    }

    public string Name { get; set; }

    public TypeNode Type { get; set; }

    public int Index { get; set; }

    public ExpressionAst? Initializer { get; set; }
}