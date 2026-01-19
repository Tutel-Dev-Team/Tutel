using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class WhileStatement : StatementAst
{
    public WhileStatement(
        ExpressionAst condition,
        StatementAst body)
    {
        Condition = condition;
        Body = body;
    }

    public ExpressionAst Condition { get; set; }

    public StatementAst Body { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}