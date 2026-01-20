using Tutel.Compiler.Lexing.TokenHandlers;
using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing;

public class Lexer
{
    private readonly ISourceReader _reader;
    private readonly ITokenHandler _handler;

    public Lexer(
        ISourceReader reader,
        ITokenHandler handler)
    {
        _reader = reader;
        _handler = handler;
    }

    public IEnumerable<Token> Tokenize()
    {
        while (_reader.MoveNext())
        {
            Token? token = _handler.Handle(_reader);
            if (token != null) yield return token;
        }

        yield return new Token(TokenType.Eof, string.Empty, _reader.Line + 1, 0);
    }
}