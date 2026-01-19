namespace Tutel.Core.Compiler.AST.Types;

public class ErrorType : TypeNode
{
    public override bool Equals(TypeNode other)
    {
        return other is ErrorType;
    }

    public override string ToString()
    {
        return "error";
    }
}
