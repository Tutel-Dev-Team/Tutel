namespace Tutel.Core.Compiler.Lexing.Abstractions;

public interface ISourceReader : IDisposable
{
    bool MoveNext();

    char Peek();

    char Current { get; }

    int Line { get; }

    int Column { get; }

    bool IsEndOfFile { get; }
}