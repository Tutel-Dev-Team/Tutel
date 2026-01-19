using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;
using Tutel.Core.Compiler.Parsing.Exception;

namespace Tutel.Core.Compiler.Parsing.Models;

public class ParseContext
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    public ParseContext(
        IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    public bool IsAtEnd => Current.Type == TokenType.Eof;

    public Token Current =>
        _position < _tokens.Count
        ? _tokens[_position]
        : Token.Eof;

    public Token Previous =>
        _position > 0
        ? _tokens[_position - 1]
        : Token.Eof;

    public Token Peek(int offset = 1)
    {
        return _position + offset < _tokens.Count
            ? _tokens[_position + offset]
            : Token.Eof;
    }

    public Token Advance()
    {
        return _position < _tokens.Count
            ? _tokens[_position++]
            : Token.Eof;
    }

    public bool Match(TokenType type)
    {
        return Current.Type == type;
    }

    public bool Match(string value)
    {
        return Current.Value == value;
    }

    public bool Match(TokenType type, string value)
    {
        return Current.Type == type && Current.Value == value;
    }

    public Token Consume(TokenType type, string errorMessage)
    {
        return Match(type)
            ? Advance()
            : throw new ParseException(errorMessage, Current.Line, Current.Column);
    }

    public Token Consume(string value, string errorMessage)
    {
        return Match(value)
            ? Advance()
            : throw new ParseException(errorMessage, Current.Line, Current.Column);
    }

    public bool TryConsume(TokenType type)
    {
        if (Match(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    public bool TryConsume(string value)
    {
        if (Match(value))
        {
            Advance();
            return true;
        }

        return false;
    }
}