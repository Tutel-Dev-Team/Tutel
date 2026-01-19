using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Expressions.Literals;

public class ArrayLiteralExpression : ExpressionAst
{
    public IReadOnlyList<ExpressionAst> Elements { get; set; } = [];

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}