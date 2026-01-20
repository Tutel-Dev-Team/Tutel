using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Expressions;

public class ArrayAccessExpression : ExpressionAst
{
    public ArrayAccessExpression(
        ExpressionAst array,
        ExpressionAst index)
    {
        Array = array;
        Index = index;
    }

    public ExpressionAst Array { get; set; }

    public ExpressionAst Index { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}