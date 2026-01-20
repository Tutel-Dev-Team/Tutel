using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST;

public abstract class AstNode
{
    public int Line { get; set; }

    public int Column { get; set; }

    public abstract T Accept<T>(IAstVisitor<T> visitor);
}