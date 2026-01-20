using Tutel.Core.Compiler.Lexing.Abstractions;

namespace Tutel.Compiler.Lexing.SourceReaders;

public class StreamSourceReader : ISourceReader
{
    private readonly StreamReader _reader;

    public StreamSourceReader(
        Stream stream)
    {
        _reader = new StreamReader(stream: stream);
    }

    public char Current { get; private set; } = '\0';

    public int Line { get; private set; } = 1;

    public int Column { get; private set; }

    public bool IsEndOfFile { get; private set; }

    public bool MoveNext()
    {
        if (IsEndOfFile) return false;

        int symbol = _reader.Read();

        if (symbol == -1)
        {
            IsEndOfFile = true;
            Current = '\0';
            return false;
        }

        Current = (char)symbol;

        if (Current == '\n')
        {
            Line++;
            Column = 1;
        }
        else
        {
            Column++;
        }

        return true;
    }

    public char Peek()
    {
        if (IsEndOfFile) return '\0';

        int nextChar = _reader.Peek();

        return nextChar == -1
            ? '\0'
            : (char)nextChar;
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}