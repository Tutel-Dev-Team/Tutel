namespace Tutel.Core.Compiler.SemanticAnalysis.Models;

public class SemanticError
{
    public string Message { get; }

    public int Line { get; }

    public SemanticError(
        string message,
        int line)
    {
        Message = message;
        Line = line;
    }

    public override string ToString() => $"[Line {Line}] {Message}";
}