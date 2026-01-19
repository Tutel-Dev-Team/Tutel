using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class ExpressionStatement : StatementAst
{
    public ExpressionStatement(ExpressionAst expression)
    {
        Expression = expression;
    }

    public ExpressionAst Expression { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}