using System.Text;
using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Exceptions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class IdentifierTokenHandler : TokenHandlerBase
{
    private readonly HashSet<string> _keywords =
    [
        "if", "else", "for", "return", "func", "while",
        "int", "double", "array", "len", "new", "break", "continue",
        "print", "read"
    ];

    public override Token? Handle(ISourceReader reader)
    {
        if (!char.IsLetter(reader.Current) && reader.Current != '_')
        {
            return Next?.Handle(reader);
        }

        int startLine = reader.Line;
        int startColumn = reader.Column;
        var builder = new StringBuilder();

        builder.Append(reader.Current);

        while (char.IsLetterOrDigit(reader.Peek()) || reader.Peek() == '_')
        {
            reader.MoveNext();
            builder.Append(reader.Current);
        }

        string identifier = builder.ToString();
        TokenType type = _keywords.Contains(identifier)
            ? TokenType.Keyword
            : TokenType.Identifier;

        if (identifier.Length > 255)
            throw new LexicalException($"Identifier too long at {startLine}:{startColumn}");

        if (identifier.StartsWith("__"))
            throw new LexicalException($"Invalid identifier name at {startLine}:{startColumn}");

        return new Token(
            type,
            identifier,
            startLine,
            startColumn);
    }
}
