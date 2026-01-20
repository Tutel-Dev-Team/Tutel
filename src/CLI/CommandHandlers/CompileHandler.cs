using Tutel.Compiler;

namespace Tutel.CLI.CommandHandlers;

public class CompileHandler : CommandHandlerBase
{
    protected override bool CanHandle(string[] args)
    {
        return args.Length > 0 && (args[0] == "compile" || args[0] == "c");
    }

    protected override int Process(string[] args)
    {
        string[] compileArgs = args.Skip(1).ToArray();

        if (compileArgs.Length == 0)
        {
            PrintCompileUsage();
            return 1;
        }

        bool disassemble = false;
        string? outputPath = null;
        string? sourcePath = null;

        for (int i = 0; i < compileArgs.Length; i++)
        {
            string arg = compileArgs[i];

            if (arg is "--help" or "-h")
            {
                PrintCompileUsage();
                return 0;
            }

            if (arg is "--disassemble" or "-d")
            {
                disassemble = true;
                continue;
            }

            if (arg is "--output" or "-o")
            {
                if (i + 1 >= compileArgs.Length)
                {
                    Console.Error.WriteLine("Error: --output requires a file path");
                    return 1;
                }

                outputPath = compileArgs[++i];
                continue;
            }

            if (arg.StartsWith("--output="))
            {
                outputPath = arg[9..];
                continue;
            }

            if (arg.StartsWith("-o"))
            {
                if (arg.Length > 2)
                {
                    outputPath = arg[2..];
                }
                else if (i + 1 < compileArgs.Length)
                {
                    outputPath = compileArgs[++i];
                }
                else
                {
                    Console.Error.WriteLine("Error: -o requires a file path");
                    return 1;
                }

                continue;
            }

            if (sourcePath == null && !arg.StartsWith('-'))
            {
                sourcePath = arg;
                continue;
            }

            Console.Error.WriteLine($"Error: Unknown option '{arg}'");
            PrintCompileUsage();
            return 1;
        }

        if (sourcePath == null)
        {
            Console.Error.WriteLine("Error: No source file specified");
            PrintCompileUsage();
            return 1;
        }

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: File '{sourcePath}' not found");
            return 1;
        }

        try
        {
            var compiler = new TutelCompiler();
            compiler.Compile(sourcePath, disassemble);

            if (outputPath != null)
            {
                string defaultOutput = Path.ChangeExtension(sourcePath, ".tbc");
                if (outputPath != defaultOutput && File.Exists(defaultOutput))
                {
                    File.Move(defaultOutput, outputPath, true);
                    Console.WriteLine($"Compiled to: {outputPath}");
                }
            }
            else
            {
                Console.WriteLine($"Compiled to: {Path.ChangeExtension(sourcePath, ".tbc")}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Compilation error: {ex.Message}");
            return 1;
        }
    }

    private void PrintCompileUsage()
    {
        Console.WriteLine("Usage: tutel compile <source.tl> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h           Show this help message");
        Console.WriteLine("  --disassemble, -d    Show disassembled bytecode");
        Console.WriteLine("  --output, -o <path>  Specify output file path");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  tutel compile program.tl");
        Console.WriteLine("  tutel compile program.tl -d -o output.tbc");
    }
}