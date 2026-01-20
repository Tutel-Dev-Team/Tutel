using Tutel.Core.Compiler.Lexing.Models.Enums;

namespace Tutel.Core.Compiler.Lexing.Models.Tokens;

public record Token(
    TokenType Type,
    string Value,
    int Line,
    int Column)
{
    public override string ToString()
    {
        return $"{Type}: {Value} ({Line}:{Column})";
    }

    public static Token Eof { get; } = new Token(TokenType.Eof, string.Empty, -1, -1);
}