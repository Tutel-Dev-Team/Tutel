using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.AST.Declarations;

public class GlobalVariableDeclaration : DeclarationAst
{
    public GlobalVariableDeclaration(
        string name,
        TypeNode type,
        ExpressionAst? initValue)
    {
        Name = name;
        Type = type;
        InitValue = initValue;
    }

    public string Name { get; set; }

    public TypeNode Type { get; set; }

    public ExpressionAst? InitValue { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}