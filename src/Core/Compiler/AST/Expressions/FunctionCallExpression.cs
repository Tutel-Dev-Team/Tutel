using Tutel.Core.Compiler.AST.Abstractions;

namespace Tutel.Core.Compiler.AST.Expressions;

public class FunctionCallExpression : ExpressionAst
{
    public FunctionCallExpression(
        string functionName,
        IReadOnlyList<ExpressionAst> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }

    public string FunctionName { get; set; }

    public IReadOnlyList<ExpressionAst> Arguments { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}