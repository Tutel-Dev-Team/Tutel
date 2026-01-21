using Tutel.Compiler.Bytecode;
using Tutel.Compiler.Lexing;
using Tutel.Compiler.Lexing.SourceReaders;
using Tutel.Compiler.Lexing.TokenHandlers;
using Tutel.Compiler.Parsing;
using Tutel.Compiler.SemanticAnalysis;
using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.Bytecode.Models;
using Tutel.Core.Compiler.Parsing.Exception;
using Tutel.Core.Compiler.Parsing.Models;

namespace Tutel.Compiler;

public class TutelCompiler
{
    public void Compile(string path, bool disassemble = false)
    {
        ITokenHandler tokenChain = new DelimiterTokenHandler()
            .AddNext(new CommentsTokenHandler())
            .AddNext(new OperatorTokenHandler())
            .AddNext(new IntegerTokenHandler())
            .AddNext(new IdentifierTokenHandler())
            .AddNext(new ErrorTokenHandler());

        using var streamReader = new StreamSourceReader(File.OpenRead(path));
        var lexer = new Lexer(streamReader, tokenChain);

        var parser = new Parser(new ParseContext(lexer.Tokenize().ToList()));
        ProgramAst program;
        try
        {
            program = parser.ParseProgram();
        }
        catch (ParseException ex)
        {
            Console.WriteLine($"Parse Error: {ex.Message}");
            return;
        }

        var analyzer = new SemanticAnalyzer();
        SymbolTable st = analyzer.Analyze(program);

        if (st.Errors.Count > 0)
        {
            Console.WriteLine("Semantic Errors:");
            Console.WriteLine(string.Join(Environment.NewLine, st.Errors));
            return;
        }

        var generator = new BytecodeGenerator(st);
        TutelBytecode bytecode = generator.Generate(program);

        string resultPath = Path.ChangeExtension(path, ".tbc");
        var writer = new BytecodeWriter();
        writer.WriteToFile(bytecode, resultPath);

        if (disassemble)
            Console.WriteLine(Disassembler.Disassemble(bytecode, st));
    }
}