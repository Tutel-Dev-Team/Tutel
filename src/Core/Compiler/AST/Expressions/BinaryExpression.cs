using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Core.Compiler.AST.Expressions;

public class BinaryExpression : ExpressionAst
{
    public BinaryExpression(
        ExpressionAst left,
        Token @operator,
        ExpressionAst right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public ExpressionAst Left { get; set; }

    public Token Operator { get; set; }

    public ExpressionAst Right { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}