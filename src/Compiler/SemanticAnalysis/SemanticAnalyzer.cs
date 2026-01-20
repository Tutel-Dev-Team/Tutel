using Tutel.Core.Compiler.AST;

namespace Tutel.Compiler.SemanticAnalysis;

public class SemanticAnalyzer
{
    public SymbolTable Analyze(ProgramAst programAst)
    {
        var tableBuilder = new SymbolTableBuilder();
        SymbolTable st = tableBuilder.Build(programAst);
        var typeChecker = new TypeChecker(st);
        var controlFlowAnalyzer = new ControlFlowAnalyzer(st);

        typeChecker.Check(programAst);
        controlFlowAnalyzer.Analyze(programAst);

        return st;
    }
}