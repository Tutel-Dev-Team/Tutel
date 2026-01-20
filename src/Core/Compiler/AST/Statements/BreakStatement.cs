using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class BreakStatement : StatementAst
{
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}