using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Core.Compiler.AST.Expressions;

public class ArrayAssignmentExpression : ExpressionAst
{
    public ArrayAssignmentExpression(
        ArrayAccessExpression target,
        Token @operator,
        ExpressionAst value)
    {
        Target = target;
        Operator = @operator;
        Value = value;
    }

    public ArrayAccessExpression Target { get; set; }

    public Token Operator { get; set; }

    public ExpressionAst Value { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}