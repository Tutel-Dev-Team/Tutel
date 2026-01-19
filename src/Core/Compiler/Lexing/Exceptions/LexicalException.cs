namespace Tutel.Core.Compiler.Lexing.Exceptions;

public class LexicalException(string message) : Exception
{
    public override string ToString()
    {
        return $"Lexical Error: {message}";
    }
}