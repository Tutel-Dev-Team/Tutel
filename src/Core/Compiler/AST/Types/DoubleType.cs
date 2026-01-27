namespace Tutel.Core.Compiler.AST.Types;

public class DoubleType : TypeNode
{
    public override bool Equals(TypeNode other)
    {
        return other is DoubleType;
    }

    public override string ToString()
    {
        return "double";
    }
}