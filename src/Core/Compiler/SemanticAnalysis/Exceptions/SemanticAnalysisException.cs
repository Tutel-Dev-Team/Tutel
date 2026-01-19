namespace Tutel.Core.Compiler.SemanticAnalysis.Exceptions;

public class SemanticAnalysisException : Exception
{
    public int Line { get; }

    public int Column { get; }

    public SemanticAnalysisException(
        string message,
        int line,
        int column)
        : base($"{message} at {line}:{column}")
    {
        Line = line;
        Column = column;
    }
}