namespace Tutel.Core.Compiler.AST.Types;

public class ArrayType : TypeNode
{
    public ArrayType(TypeNode elementType)
    {
        ElementType = elementType;
    }

    public TypeNode ElementType { get; set; }

    public override bool Equals(TypeNode other)
    {
        return other is ArrayType arrayType && ElementType.Equals(arrayType.ElementType);
    }

    public override string ToString()
    {
        return $"{ElementType}[]";
    }
}