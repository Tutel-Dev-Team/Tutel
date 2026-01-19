using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public interface ITokenHandler
{
    ITokenHandler AddNext(ITokenHandler handler);

    Token? Handle(ISourceReader reader);
}