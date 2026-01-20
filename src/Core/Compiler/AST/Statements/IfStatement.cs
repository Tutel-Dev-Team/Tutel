using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class IfStatement : StatementAst
{
    public IfStatement(
        ExpressionAst condition,
        StatementAst thenBranch,
        StatementAst? elseBranch)
    {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }

    public ExpressionAst Condition { get; set; }

    public StatementAst ThenBranch { get; set; }

    public StatementAst? ElseBranch { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}