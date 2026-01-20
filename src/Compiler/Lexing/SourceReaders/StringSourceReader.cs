using Tutel.Core.Compiler.Lexing.Abstractions;

namespace Tutel.Compiler.Lexing.SourceReaders;

public class StringSourceReader : ISourceReader
{
    private readonly string _source;
    private int _position = -1;

    public StringSourceReader(string source)
    {
        _source = source;
    }

    public char Current => _position >= 0 && _position < _source.Length
        ? _source[_position]
        : '\0';

    public int Line { get; private set; } = 1;

    public int Column { get; private set; } = 1;

    public bool IsEndOfFile => _position >= _source.Length;

    public bool MoveNext()
    {
        if (_position >= _source.Length - 1)
            return false;

        _position++;

        if (_position < _source.Length)
        {
            if (Current == '\n')
            {
                Line++;
                Column = 0;
            }
            else
            {
                Column = _position == 0 ? 1 : Column + 1;
            }
        }

        return true;
    }

    public char Peek()
    {
        return _position >= _source.Length - 1
            ? '\0'
            : _source[_position + 1];
    }

    public void Dispose() { }
}