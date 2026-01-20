using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Expressions;

public class LengthExpression : ExpressionAst
{
    public LengthExpression(ExpressionAst array)
    {
        Array = array;
    }

    public ExpressionAst Array { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}