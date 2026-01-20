using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class DelimiterTokenHandler : TokenHandlerBase
{
    private readonly List<char> _delimiters = [';', '(', ')', '{', '}', ',', '[', ']'];

    public override Token? Handle(ISourceReader reader)
    {
        return _delimiters.Contains(reader.Current)
            ? new Token(
                TokenType.Delimiter,
                reader.Current.ToString(),
                reader.Line,
                reader.Column)
            : Next?.Handle(reader);
    }
}