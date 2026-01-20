using System.Collections.ObjectModel;
using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Statements;

public class PrintStatement : StatementAst
{
    public PrintStatement(Collection<ExpressionAst> expressions)
    {
        Expressions = expressions;
    }

    public Collection<ExpressionAst> Expressions { get; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}