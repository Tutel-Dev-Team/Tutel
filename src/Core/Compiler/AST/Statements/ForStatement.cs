using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class ForStatement : StatementAst
{
    public ForStatement(StatementAst body)
    {
        Body = body;
    }

    public StatementAst? Initializer { get; set; }

    public ExpressionAst? Condition { get; set; }

    public ExpressionAst? Increment { get; set; }

    public StatementAst Body { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}