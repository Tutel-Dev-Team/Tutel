using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Expressions;

public class IdentifierExpression : ExpressionAst
{
    public IdentifierExpression(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}