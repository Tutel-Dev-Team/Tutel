namespace Tutel.Core.Compiler.AST.Types;

public class IntType : TypeNode
{
    public override bool Equals(TypeNode other)
    {
        return other is IntType;
    }

    public override string ToString()
    {
        return "int";
    }
}