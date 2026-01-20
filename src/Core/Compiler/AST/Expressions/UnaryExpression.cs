using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Core.Compiler.AST.Expressions;

public class UnaryExpression : ExpressionAst
{
    public UnaryExpression(
        Token @operator,
        ExpressionAst operand)
    {
        Operator = @operator;
        Operand = operand;
    }

    public Token Operator { get; set; }

    public ExpressionAst Operand { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}