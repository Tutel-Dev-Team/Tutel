using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.AST.Statements;

public class VariableDeclarationStatement : StatementAst
{
    public VariableDeclarationStatement(
        string name,
        TypeNode type,
        ExpressionAst? initializer = null)
    {
        Name = name;
        Type = type;
        InitValue = initializer;
    }

    public string Name { get; set; }

    public TypeNode Type { get; set; }

    public ExpressionAst? InitValue { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}