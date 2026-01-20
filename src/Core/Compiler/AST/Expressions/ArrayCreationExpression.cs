using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.AST.Expressions;

public class ArrayCreationExpression : ExpressionAst
{
    public ArrayCreationExpression(
        TypeNode elementType,
        TypeNode arrayType,
        ExpressionAst size)
    {
        ElementType = elementType;
        ArrayType = arrayType;
        Size = size;
    }

    public TypeNode ElementType { get; set; }

    public TypeNode ArrayType { get; set; }

    public ExpressionAst Size { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}