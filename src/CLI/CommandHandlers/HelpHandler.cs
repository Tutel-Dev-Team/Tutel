namespace Tutel.CLI.CommandHandlers;

public class HelpHandler : CommandHandlerBase
{
    protected override bool CanHandle(string[] args)
    {
        return args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help");
    }

    protected override int Process(string[] args)
    {
        PrintUsage();
        return 0;
    }

    protected override void PrintUsage()
    {
        Console.WriteLine("Tutel Language Tools");
        Console.WriteLine();
        Console.WriteLine("Usage: tutel <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile <file.tl>     Compile source file to bytecode");
        Console.WriteLine("  run <file.tbc>        Run bytecode file");
        Console.WriteLine("  build-run <file.tl>   Compile and run source file");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h     Show this help message");
        Console.WriteLine("  --version, -v  Show version information");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  tutel compile program.tl -d");
        Console.WriteLine("  tutel run program.tbc --debug");
        Console.WriteLine("  tutel build-run program.tl");
    }
}