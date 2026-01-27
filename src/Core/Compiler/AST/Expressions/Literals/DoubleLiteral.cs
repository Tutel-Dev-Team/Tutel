using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Expressions.Literals;

public class DoubleLiteral : ExpressionAst
{
    public double Value { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}