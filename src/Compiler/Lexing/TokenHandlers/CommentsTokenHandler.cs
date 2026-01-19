using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Exceptions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class CommentsTokenHandler : TokenHandlerBase
{
    public override Token? Handle(ISourceReader reader)
    {
        if (reader.Current != '/')
        {
            return Next?.Handle(reader);
        }

        int startLine = reader.Line;
        int startColumn = reader.Column;

        char nextChar = reader.Peek();

        if (nextChar is not ('*' or '/')) return Next?.Handle(reader);

        switch (nextChar)
        {
            case '/':
                while (reader.MoveNext())
                {
                    if (reader.Current == '\n') break;
                }

                break;
            case '*':
                while (reader.MoveNext() && !reader.IsEndOfFile)
                {
                    if (reader.Current == '*' && reader.Peek() == '/')
                    {
                        reader.MoveNext();
                        break;
                    }
                }

                if (reader.IsEndOfFile)
                    throw new LexicalException($"Unclosed multi-line comment at {startLine}:{startColumn}");

                break;
        }

        return null;
    }
}