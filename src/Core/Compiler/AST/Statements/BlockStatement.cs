using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class BlockStatement : StatementAst
{
    public IReadOnlyList<StatementAst> Statements { get; set; } = [];

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}