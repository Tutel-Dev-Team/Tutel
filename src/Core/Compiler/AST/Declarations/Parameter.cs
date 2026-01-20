using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.AST.Declarations;

public class Parameter
{
    public Parameter(
        string name,
        TypeNode type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; set; }

    public TypeNode Type { get; set; }
}