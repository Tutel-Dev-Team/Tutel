using System.Text;
using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Exceptions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class IntegerTokenHandler : TokenHandlerBase
{
    public override Token? Handle(ISourceReader reader)
    {
        if (!char.IsDigit(reader.Current))
        {
            return Next?.Handle(reader);
        }

        int startLine = reader.Line;
        int startColumn = reader.Column;
        var builder = new StringBuilder();

        builder.Append(reader.Current);

        while (char.IsDigit(reader.Peek()))
        {
            reader.MoveNext();
            builder.Append(reader.Current);
        }

        string numberStr = builder.ToString();

        if (numberStr.Length > 20)
            throw new LexicalException($"Number too large at {startLine}:{startColumn}");

        if (!long.TryParse(numberStr, out _))
            throw new LexicalException($"Invalid integer literal at {startLine}:{startColumn}");

        return new Token(
            TokenType.IntegerType,
            numberStr,
            startLine,
            startColumn);
    }
}