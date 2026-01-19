using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;

namespace Tutel.Core.Compiler.AST.Abstractions;

public interface IAstVisitor<T>
{
    T Visit(ProgramAst programAst);

    // Выражения
    T Visit(IntegerLiteral expr);

    T Visit(IdentifierExpression expr);

    T Visit(BinaryExpression expr);

    T Visit(UnaryExpression expr);

    T Visit(FunctionCallExpression expr);

    T Visit(ArrayAccessExpression expr);

    T Visit(ArrayCreationExpression expr);

    T Visit(ArrayLiteralExpression expr);

    T Visit(LengthExpression expr);

    T Visit(AssignmentExpression expr);

    T Visit(ArrayAssignmentExpression expr);

    T Visit(ReadExpression expr);

    // Операторы
    T Visit(BlockStatement stmt);

    T Visit(VariableDeclarationStatement stmt);

    T Visit(IfStatement stmt);

    T Visit(WhileStatement stmt);

    T Visit(ForStatement stmt);

    T Visit(ReturnStatement stmt);

    T Visit(BreakStatement stmt);

    T Visit(ContinueStatement stmt);

    T Visit(ExpressionStatement stmt);

    T Visit(PrintStatement stmt);

    // Объявления
    T Visit(FunctionDeclaration decl);

    T Visit(GlobalVariableDeclaration decl);

    // Типы
    T Visit(IntType type);

    T Visit(ArrayType type);

    T Visit(VoidType type);
}