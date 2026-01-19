using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Exceptions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class ErrorTokenHandler : TokenHandlerBase
{
    public override Token? Handle(ISourceReader reader)
    {
        return !IsValidCharacter(reader.Current)
            ? throw new LexicalException($"Unknown character: {reader.Current} at {reader.Line}:{reader.Column}")
            : null;
    }

    private bool IsValidCharacter(char c)
    {
        return char.IsLetterOrDigit(c) ||
               char.IsWhiteSpace(c) ||
               "_+-*/=<>!&|^%?,;()[]{}".Contains(c, StringComparison.CurrentCulture);
    }
}