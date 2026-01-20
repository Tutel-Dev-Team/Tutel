namespace Tutel.Core.Compiler.AST.Types;

public class VoidType : TypeNode
{
    public override bool Equals(TypeNode other)
    {
        return other is VoidType;
    }

    public override string ToString()
    {
        return "void";
    }
}