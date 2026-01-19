namespace Tutel.Core.Compiler.Parsing.Exception;

public class ParseException : System.Exception
{
    public int Line { get; }

    public int Column { get; }

    public ParseException(
        string message,
        int line,
        int column)
        : base($"{message} at {line}:{column}")
    {
        Line = line;
        Column = column;
    }
}