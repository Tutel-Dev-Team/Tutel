namespace Tutel.Core.Compiler.AST.Types;

public abstract class TypeNode
{
    public abstract bool Equals(TypeNode other);

    public abstract override string ToString();
}