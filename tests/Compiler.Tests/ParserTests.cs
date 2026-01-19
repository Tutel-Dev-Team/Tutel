using Tutel.Compiler.Parsing;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;
using Tutel.Core.Compiler.Parsing.Models;
using Xunit;

namespace Compiler.Tests;

public class ParserTests
{
    [Fact]
    public void ParseProgram_EmptyProgram_ReturnsEmptyProgram()
    {
        // Arrange
        List<Token> tokens = CreateTokens();
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Empty(program.Declarations);
    }

    [Fact]
    public void ParseVariableDeclaration_IntVariable_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "int", 1, 1),
            (TokenType.Identifier, "x", 1, 5),
            (TokenType.Delimiter, ";", 1, 6));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        Assert.Single(program.Declarations);
        GlobalVariableDeclaration declaration = Assert.IsType<GlobalVariableDeclaration>(program.Declarations[0]);
        Assert.Equal("x", declaration.Name);
        Assert.IsType<IntType>(declaration.Type);
        Assert.Null(declaration.InitValue);
    }

    [Fact]
    public void ParseVariableDeclaration_IntVariableWithInitializer_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "int", 1, 1),
            (TokenType.Identifier, "x", 1, 5),
            (TokenType.Operator, "=", 1, 7),
            (TokenType.IntegerType, "42", 1, 9),
            (TokenType.Delimiter, ";", 1, 11));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        GlobalVariableDeclaration declaration = Assert.IsType<GlobalVariableDeclaration>(program.Declarations[0]);
        Assert.NotNull(declaration.InitValue);
        IntegerLiteral initializer = Assert.IsType<IntegerLiteral>(declaration.InitValue);
        Assert.Equal(42, initializer.Value);
    }

    [Fact]
    public void ParseFunctionDeclaration_SimpleFunction_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "int", 1, 6),
            (TokenType.Identifier, "main", 1, 11),
            (TokenType.Delimiter, "(", 1, 15),
            (TokenType.Delimiter, ")", 1, 16),
            (TokenType.Delimiter, "{", 1, 18),
            (TokenType.Delimiter, "}", 1, 19));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration declaration = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        Assert.Equal("main", declaration.Name);
        Assert.IsType<IntType>(declaration.ReturnType);
        Assert.Empty(declaration.Parameters);
        Assert.NotNull(declaration.Body);
    }

    [Fact]
    public void ParseFunctionDeclaration_FunctionWithParameters_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "int", 1, 6),
            (TokenType.Identifier, "add", 1, 10),
            (TokenType.Delimiter, "(", 1, 13),
            (TokenType.Keyword, "int", 1, 14),
            (TokenType.Identifier, "a", 1, 18),
            (TokenType.Delimiter, ",", 1, 19),
            (TokenType.Keyword, "int", 1, 21),
            (TokenType.Identifier, "b", 1, 25),
            (TokenType.Delimiter, ")", 1, 26),
            (TokenType.Delimiter, "{", 1, 28),
            (TokenType.Keyword, "return", 1, 30),
            (TokenType.Identifier, "a", 1, 37),
            (TokenType.Operator, "+", 1, 39),
            (TokenType.Identifier, "b", 1, 41),
            (TokenType.Delimiter, ";", 1, 42),
            (TokenType.Delimiter, "}", 1, 44));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration declaration = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        Assert.Equal(2, declaration.Parameters.Count);
        Assert.Equal("a", declaration.Parameters[0].Name);
        Assert.Equal("b", declaration.Parameters[1].Name);
        Assert.IsType<IntType>(declaration.Parameters[0].Type);
    }

    [Fact]
    public void ParseExpression_ArithmeticExpression_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "int", 1, 1),
            (TokenType.Identifier, "x", 1, 5),
            (TokenType.Operator, "=", 1, 7),
            (TokenType.IntegerType, "1", 1, 9),
            (TokenType.Operator, "+", 1, 11),
            (TokenType.IntegerType, "2", 1, 13),
            (TokenType.Operator, "*", 1, 15),
            (TokenType.IntegerType, "3", 1, 17),
            (TokenType.Delimiter, ";", 1, 18));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        GlobalVariableDeclaration declaration = Assert.IsType<GlobalVariableDeclaration>(program.Declarations[0]);
        BinaryExpression binaryExpr = Assert.IsType<BinaryExpression>(declaration.InitValue);
        Assert.Equal("+", binaryExpr.Operator.Value);
    }

    [Fact]
    public void ParseArrayType_IntArray_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "int", 1, 1),
            (TokenType.Delimiter, "[", 1, 5),
            (TokenType.Delimiter, "]", 1, 6),
            (TokenType.Identifier, "arr", 1, 8),
            (TokenType.Delimiter, ";", 1, 11));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        GlobalVariableDeclaration declaration = Assert.IsType<GlobalVariableDeclaration>(program.Declarations[0]);
        ArrayType arrayType = Assert.IsType<ArrayType>(declaration.Type);
        Assert.IsType<IntType>(arrayType.ElementType);
    }

    [Fact]
    public void ParseArrayCreation_NewIntArray_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "int", 1, 1),
            (TokenType.Identifier, "arr", 1, 5),
            (TokenType.Operator, "=", 1, 9),
            (TokenType.Keyword, "new", 1, 11),
            (TokenType.Keyword, "int", 1, 15),
            (TokenType.Delimiter, "[", 1, 19),
            (TokenType.IntegerType, "10", 1, 20),
            (TokenType.Delimiter, "]", 1, 22),
            (TokenType.Delimiter, ";", 1, 23));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        GlobalVariableDeclaration declaration = Assert.IsType<GlobalVariableDeclaration>(program.Declarations[0]);
        ArrayCreationExpression arrayCreation = Assert.IsType<ArrayCreationExpression>(declaration.InitValue);
        Assert.IsType<IntType>(arrayCreation.ElementType);
        IntegerLiteral size = Assert.IsType<IntegerLiteral>(arrayCreation.Size);
        Assert.Equal(10, size.Value);
    }

    [Fact]
    public void ParseIfStatement_IfElse_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "void", 1, 6),
            (TokenType.Identifier, "test", 1, 11),
            (TokenType.Delimiter, "(", 1, 15),
            (TokenType.Delimiter, ")", 1, 16),
            (TokenType.Delimiter, "{", 1, 18),
            (TokenType.Keyword, "if", 1, 20),
            (TokenType.Delimiter, "(", 1, 23),
            (TokenType.Identifier, "x", 1, 24),
            (TokenType.Operator, ">", 1, 26),
            (TokenType.IntegerType, "0", 1, 28),
            (TokenType.Delimiter, ")", 1, 29),
            (TokenType.Keyword, "return", 1, 31),
            (TokenType.IntegerType, "1", 1, 38),
            (TokenType.Delimiter, ";", 1, 39),
            (TokenType.Keyword, "else", 1, 41),
            (TokenType.Keyword, "return", 1, 46),
            (TokenType.IntegerType, "0", 1, 53),
            (TokenType.Delimiter, ";", 1, 54),
            (TokenType.Delimiter, "}", 1, 56));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        BlockStatement block = func.Body;
        IfStatement ifStmt = Assert.IsType<IfStatement>(block.Statements[0]);
        Assert.NotNull(ifStmt.ThenBranch);
        Assert.NotNull(ifStmt.ElseBranch);
    }

    [Fact]
    public void ParseWhileStatement_WhileLoop_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "void", 1, 6),
            (TokenType.Identifier, "test", 1, 11),
            (TokenType.Delimiter, "(", 1, 15),
            (TokenType.Delimiter, ")", 1, 16),
            (TokenType.Delimiter, "{", 1, 18),
            (TokenType.Keyword, "while", 1, 20),
            (TokenType.Delimiter, "(", 1, 26),
            (TokenType.Identifier, "x", 1, 27),
            (TokenType.Operator, "<", 1, 29),
            (TokenType.IntegerType, "10", 1, 31),
            (TokenType.Delimiter, ")", 1, 33),
            (TokenType.Delimiter, "{", 1, 35),
            (TokenType.Identifier, "x", 1, 37),
            (TokenType.Operator, "=", 1, 39),
            (TokenType.Identifier, "x", 1, 41),
            (TokenType.Operator, "+", 1, 43),
            (TokenType.IntegerType, "1", 1, 45),
            (TokenType.Delimiter, ";", 1, 46),
            (TokenType.Delimiter, "}", 1, 48),
            (TokenType.Delimiter, "}", 1, 50));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        BlockStatement block = func.Body;
        WhileStatement whileStmt = Assert.IsType<WhileStatement>(block.Statements[0]);
        Assert.NotNull(whileStmt.Condition);
        Assert.NotNull(whileStmt.Body);
    }

    [Fact]
    public void ParseForStatement_ForLoop_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "void", 1, 6),
            (TokenType.Identifier, "test", 1, 11),
            (TokenType.Delimiter, "(", 1, 15),
            (TokenType.Delimiter, ")", 1, 16),
            (TokenType.Delimiter, "{", 1, 18),
            (TokenType.Keyword, "for", 1, 20),
            (TokenType.Delimiter, "(", 1, 24),
            (TokenType.Keyword, "int", 1, 25),
            (TokenType.Identifier, "i", 1, 29),
            (TokenType.Operator, "=", 1, 31),
            (TokenType.IntegerType, "0", 1, 33),
            (TokenType.Delimiter, ";", 1, 34),
            (TokenType.Identifier, "i", 1, 36),
            (TokenType.Operator, "<", 1, 38),
            (TokenType.IntegerType, "10", 1, 40),
            (TokenType.Delimiter, ";", 1, 42),
            (TokenType.Identifier, "i", 1, 44),
            (TokenType.Operator, "=", 1, 46),
            (TokenType.Identifier, "i", 1, 48),
            (TokenType.Operator, "+", 1, 50),
            (TokenType.IntegerType, "1", 1, 52),
            (TokenType.Delimiter, ")", 1, 53),
            (TokenType.Delimiter, "{", 1, 55),
            (TokenType.Delimiter, "}", 1, 56),
            (TokenType.Delimiter, "}", 1, 58));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        BlockStatement block = func.Body;
        ForStatement forStmt = Assert.IsType<ForStatement>(block.Statements[0]);
        Assert.NotNull(forStmt.Initializer);
        Assert.NotNull(forStmt.Condition);
        Assert.NotNull(forStmt.Increment);
    }

    [Fact]
    public void ParseFunctionCall_FunctionCallExpression_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "void", 1, 6),
            (TokenType.Identifier, "test", 1, 11),
            (TokenType.Delimiter, "(", 1, 15),
            (TokenType.Delimiter, ")", 1, 16),
            (TokenType.Delimiter, "{", 1, 18),
            (TokenType.Identifier, "print", 1, 20),
            (TokenType.Delimiter, "(", 1, 25),
            (TokenType.Identifier, "x", 1, 26),
            (TokenType.Delimiter, ",", 1, 27),
            (TokenType.IntegerType, "42", 1, 29),
            (TokenType.Delimiter, ")", 1, 31),
            (TokenType.Delimiter, ";", 1, 32),
            (TokenType.Delimiter, "}", 1, 34));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        BlockStatement block = func.Body;
        PrintStatement printStmt = Assert.IsType<PrintStatement>(block.Statements[0]);
        Assert.Equal(2, printStmt.Expressions.Count);
    }

    [Fact]
    public void ParseArrayAccess_ArrayElementAccess_ReturnsCorrectAST()
    {
        // Arrange
        List<Token> tokens = CreateTokens(
            (TokenType.Keyword, "func", 1, 1),
            (TokenType.Keyword, "void", 1, 6),
            (TokenType.Identifier, "test", 1, 11),
            (TokenType.Delimiter, "(", 1, 15),
            (TokenType.Delimiter, ")", 1, 16),
            (TokenType.Delimiter, "{", 1, 18),
            (TokenType.Identifier, "arr", 1, 20),
            (TokenType.Delimiter, "[", 1, 23),
            (TokenType.IntegerType, "0", 1, 24),
            (TokenType.Delimiter, "]", 1, 25),
            (TokenType.Operator, "=", 1, 27),
            (TokenType.IntegerType, "42", 1, 29),
            (TokenType.Delimiter, ";", 1, 31),
            (TokenType.Delimiter, "}", 1, 33));
        var context = new ParseContext(tokens);
        var parser = new Parser(context);

        // Act
        Tutel.Core.Compiler.AST.ProgramAst program = parser.ParseProgram();

        // Assert
        FunctionDeclaration func = Assert.IsType<FunctionDeclaration>(program.Declarations[0]);
        BlockStatement block = func.Body;
        ExpressionStatement exprStmt = Assert.IsType<ExpressionStatement>(block.Statements[0]);
        ArrayAssignmentExpression assignment = Assert.IsType<ArrayAssignmentExpression>(exprStmt.Expression);
        ArrayAccessExpression arrayAccess = Assert.IsType<ArrayAccessExpression>(assignment.Target);
        Assert.IsType<IdentifierExpression>(arrayAccess.Array);
    }

    private List<Token> CreateTokens(params (TokenType Type, string Value, int Line, int Col)[] tokens)
    {
        var result = new List<Token>();
        foreach ((TokenType type, string value, int line, int col) in tokens)
        {
            result.Add(new Token(type, value, line, col));
        }

        result.Add(new Token(TokenType.Eof, string.Empty, -1, -1));
        return result;
    }
}