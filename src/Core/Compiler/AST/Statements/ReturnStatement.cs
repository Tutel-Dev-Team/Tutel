using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class ReturnStatement : StatementAst
{
    public ExpressionAst? Value { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}