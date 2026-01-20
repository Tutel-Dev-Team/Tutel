using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public abstract class TokenHandlerBase : ITokenHandler
{
    protected ITokenHandler? Next { get; private set; }

    public ITokenHandler AddNext(ITokenHandler handler)
    {
        if (Next == null)
        {
            Next = handler;
        }
        else
        {
            Next.AddNext(handler);
        }

        return this;
    }

    public abstract Token? Handle(ISourceReader reader);
}