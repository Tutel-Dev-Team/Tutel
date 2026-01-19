using Tutel.Compiler.Lexing;
using Tutel.Compiler.Lexing.SourceReaders;
using Tutel.Compiler.Lexing.TokenHandlers;
using Tutel.Core.Compiler.Lexing.Exceptions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;
using Xunit;

namespace Compiler.Tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_EmptySource_ReturnsOnlyEof()
    {
        // Act
        List<Token> tokens = Tokenize(string.Empty);

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenType.Eof, tokens[0].Type);
        Assert.Equal(string.Empty, tokens[0].Value);
    }

    [Fact]
    public void Tokenize_SingleIdentifier_ReturnsIdentifierToken()
    {
        // Act
        List<Token> tokens = Tokenize("variable");

            // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("variable", tokens[0].Value);
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
    }

    [Fact]
    public void Tokenize_Keyword_ReturnsKeywordToken()
    {
        // Act
        List<Token> tokens = Tokenize("if");

        // Assert
        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("if", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_MultipleKeywords_ReturnsCorrectTokens()
    {
        // Act
        List<Token> tokens = Tokenize("if else while");

        // Assert
        Assert.Equal(4, tokens.Count);
        Assert.Equal("if", tokens[0].Value);
        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("else", tokens[1].Value);
        Assert.Equal(TokenType.Keyword, tokens[1].Type);
        Assert.Equal("while", tokens[2].Value);
        Assert.Equal(TokenType.Keyword, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_IntegerLiteral_ReturnsIntegerToken()
    {
        // Act
        List<Token> tokens = Tokenize("123");

        // Assert
        Assert.Equal(TokenType.IntegerType, tokens[0].Type);
        Assert.Equal("123", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_MultipleIntegers_ReturnsCorrectTokens()
    {
        // Act
        List<Token> tokens = Tokenize("42 100 999");

        // Assert
        Assert.Equal(4, tokens.Count);
        Assert.Equal("42", tokens[0].Value);
        Assert.Equal("100", tokens[1].Value);
        Assert.Equal("999", tokens[2].Value);
    }

    [Fact]
    public void Tokenize_Delimiter_ReturnsDelimiterToken()
    {
        // Act
        List<Token> tokens = Tokenize(";");

        // Assert
        Assert.Equal(TokenType.Delimiter, tokens[0].Type);
        Assert.Equal(";", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_AllDelimiters_ReturnsCorrectTokens()
    {
        // Arrange
        string delimiters = ";(){}[],";

        // Act
        List<Token> tokens = Tokenize(delimiters);

        // Assert
        Assert.Equal(delimiters.Length + 1, tokens.Count);
        for (int i = 0; i < delimiters.Length; i++)
        {
            Assert.Equal(TokenType.Delimiter, tokens[i].Type);
            Assert.Equal(delimiters[i].ToString(), tokens[i].Value);
        }
    }

    [Fact]
    public void Tokenize_Operators_ReturnsOperatorTokens()
    {
        // Act
        List<Token> tokens = Tokenize("+ - * / = < > == != <= >= && ||");

        // Assert
        string[] expected = { "+", "-", "*", "/", "=", "<", ">", "==", "!=", "<=", ">=", "&&", "||" };

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(TokenType.Operator, tokens[i].Type);
            Assert.Equal(expected[i], tokens[i].Value);
        }
    }

    [Fact]
    public void Tokenize_SingleLineComment_CommentIsIgnored()
    {
        // Act
        List<Token> tokens = Tokenize("// comment\nx");

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal("x", tokens[0].Value);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(2, tokens[0].Line);
    }

    [Fact]
    public void Tokenize_MultiLineComment_CommentIsIgnored()
    {
        // Act
        List<Token> tokens = Tokenize("/* comment */x");

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal("x", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_MultiLineCommentWithNewlines_CommentIsIgnored()
    {
        // Act
        List<Token> tokens = Tokenize("/* line1\nline2 */x");

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal("x", tokens[0].Value);
        Assert.Equal(2, tokens[0].Line);
    }

    [Fact]
    public void Tokenize_ExpressionWithAllTokenTypes_ReturnsCorrectTokens()
    {
        // Act
        List<Token> tokens = Tokenize("if (x == 42) { return y + 1; }");

        // Assert
        Token[] expected = new[]
        {
            new Token(TokenType.Keyword, "if", 1, 1),
            new Token(TokenType.Delimiter, "(", 1, 4),
            new Token(TokenType.Identifier, "x", 1, 5),
            new Token(TokenType.Operator, "==", 1, 7),
            new Token(TokenType.IntegerType, "42", 1, 10),
            new Token(TokenType.Delimiter, ")", 1, 12),
            new Token(TokenType.Delimiter, "{", 1, 14),
            new Token(TokenType.Keyword, "return", 1, 16),
            new Token(TokenType.Identifier, "y", 1, 23),
            new Token(TokenType.Operator, "+", 1, 25),
            new Token(TokenType.IntegerType, "1", 1, 27),
            new Token(TokenType.Delimiter, ";", 1, 28),
            new Token(TokenType.Delimiter, "}", 1, 30),
        };

        Assert.Equal(expected.Length + 1, tokens.Count);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Type, tokens[i].Type);
            Assert.Equal(expected[i].Value, tokens[i].Value);
            Assert.Equal(expected[i].Line, tokens[i].Line);
            Assert.Equal(expected[i].Column, tokens[i].Column);
        }
    }

    [Fact]
    public void Tokenize_IdentifierWithUnderscore_ReturnsIdentifierToken()
    {
        // Act
        List<Token> tokens = Tokenize("my_variable");

        // Assert
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("my_variable", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_IdentifierWithDigits_ReturnsIdentifierToken()
    {
        // Act
        List<Token> tokens = Tokenize("var123");

        // Assert
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("var123", tokens[0].Value);
    }

    [Theory]
    [InlineData("__invalid")]
    [InlineData("__test")]
    public void Tokenize_IdentifierStartsWithDoubleUnderscore_ThrowsException(string identifier)
    {
        // Act & Assert
        Assert.Throws<LexicalException>(() => Tokenize(identifier));
    }

    [Fact]
    public void Tokenize_IdentifierTooLong_ThrowsException()
    {
        // Arrange
        string longIdentifier = new string('a', 256);

        // Act & Assert
        Assert.Throws<LexicalException>(() => Tokenize(longIdentifier));
    }

    [Fact]
    public void Tokenize_IntegerTooLarge_ThrowsException()
    {
        // Arrange
        string hugeNumber = new string('9', 21);

        // Act & Assert
        Assert.Throws<LexicalException>(() => Tokenize(hugeNumber));
    }

    [Fact]
    public void Tokenize_InvalidInteger_ThrowsException()
    {
        // Arrange
        string overflowingNumber = "99999999999999999999";

        // Act & Assert
        Assert.Throws<LexicalException>(() => Tokenize(overflowingNumber));
    }

    [Theory]
    [InlineData("@")]
    [InlineData("#")]
    [InlineData("$")]
    [InlineData("`")]
    public void Tokenize_InvalidCharacter_ThrowsException(string invalidChar)
    {
        // Act & Assert
        Assert.Throws<LexicalException>(() => Tokenize(invalidChar));
    }

    [Fact]
    public void Tokenize_ComplexExpression_PreservesCorrectPositions()
    {
        // Arrange
        string source = @"func calculate(a, b) {
    if (a > b) {
        return a + b * 2;
    }
    return 0;
}";

        // Act
        List<Token> tokens = Tokenize(source);
        tokens = tokens.Take(tokens.Count - 1).ToList();

        // Assert key positions
        Assert.Equal("func", tokens[0].Value);
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);

        Assert.Equal("calculate", tokens[1].Value);
        Assert.Equal(1, tokens[1].Line);
        Assert.Equal(6, tokens[1].Column);

        Assert.Equal("(", tokens[2].Value);
        Assert.Equal(1, tokens[2].Line);
        Assert.Equal(15, tokens[2].Column);

        Assert.Equal("if", tokens[8].Value);
        Assert.Equal(2, tokens[8].Line);
        Assert.Equal(5, tokens[8].Column);

        Assert.Equal("return", tokens[15].Value);
        Assert.Equal(3, tokens[15].Line);
        Assert.Equal(9, tokens[15].Column);
    }

    [Fact]
    public void Tokenize_OperatorFollowedByIdentifier_ReturnsSeparateTokens()
    {
        // Act
        List<Token> tokens = Tokenize("x+y");

        // Assert
        Assert.Equal(4, tokens.Count);
        Assert.Equal("x", tokens[0].Value);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);

        Assert.Equal("+", tokens[1].Value);
        Assert.Equal(TokenType.Operator, tokens[1].Type);

        Assert.Equal("y", tokens[2].Value);
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_NumberFollowedByIdentifier_ReturnsSeparateTokens()
    {
        // Act
        List<Token> tokens = Tokenize("123abc");

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Equal("123", tokens[0].Value);
        Assert.Equal(TokenType.IntegerType, tokens[0].Type);

        Assert.Equal("abc", tokens[1].Value);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
    }

    private static ITokenHandler CreateChainOfHandlers()
    {
        return new CommentsTokenHandler()
            .AddNext(new DelimiterTokenHandler())
            .AddNext(new OperatorTokenHandler())
            .AddNext(new IntegerTokenHandler())
            .AddNext(new IdentifierTokenHandler())
            .AddNext(new ErrorTokenHandler());
    }

    private List<Token> Tokenize(string source)
    {
        using var reader = new StringSourceReader(source);
        var lexer = new Lexer(reader, CreateChainOfHandlers());
        return lexer.Tokenize().ToList();
    }
}