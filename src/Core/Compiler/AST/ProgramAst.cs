using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST;

public class ProgramAst : AstNode
{
    public IReadOnlyList<DeclarationAst> Declarations { get; set; } = [];

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}