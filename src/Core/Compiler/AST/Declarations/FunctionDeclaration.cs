using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.AST.Declarations;

public class FunctionDeclaration : DeclarationAst
{
    public FunctionDeclaration(
        string name,
        BlockStatement body,
        TypeNode returnType)
    {
        Name = name;
        Body = body;
        ReturnType = returnType;
    }

    public string Name { get; set; }

    public IReadOnlyList<Parameter> Parameters { get; set; } = [];

    public TypeNode ReturnType { get; set; }

    public BlockStatement Body { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}