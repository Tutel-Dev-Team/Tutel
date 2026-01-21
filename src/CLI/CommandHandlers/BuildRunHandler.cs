using Tutel.Compiler;
using Tutel.VirtualMachine.CLI;

namespace Tutel.CLI.CommandHandlers;

public class BuildRunHandler : CommandHandlerBase
{
    protected override bool CanHandle(string[] args)
    {
        return args.Length > 0 && (args[0] == "build-run" || args[0] == "br");
    }

    protected override int Process(string[] args)
    {
        string[] buildRunArgs = args.Skip(1).ToArray();

        if (buildRunArgs.Length == 0)
        {
            PrintBuildRunUsage();
            return 1;
        }

        var compilerArgs = new List<string>();
        var vmArgs = new List<string>();
        string? sourcePath = null;
        bool separatorFound = false;

        for (int i = 0; i < buildRunArgs.Length; i++)
        {
            string arg = buildRunArgs[i];

            if (arg is "--help" or "-h")
            {
                PrintBuildRunUsage();
                return 0;
            }

            if (arg.StartsWith("--"))
            {
                separatorFound = true;
            }

            if (!separatorFound)
            {
                if (sourcePath == null && !arg.StartsWith('-'))
                {
                    sourcePath = arg;
                }
                else
                {
                    compilerArgs.Add(arg);
                }
            }
            else
            {
                vmArgs.Add(arg);
            }
        }

        if (sourcePath == null)
        {
            Console.Error.WriteLine("Error: No source file specified");
            PrintBuildRunUsage();
            return 1;
        }

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: File '{sourcePath}' not found");
            return 1;
        }

        try
        {
            string tempDir = Path.GetTempPath();
            string tempFile = Path.Combine(tempDir, $"tutel_temp_{Guid.NewGuid():N}.tbc");

            try
            {
                var compiler = new TutelCompiler();
                compiler.Compile(sourcePath);

                string defaultOutput = Path.ChangeExtension(sourcePath, ".tbc");
                if (File.Exists(defaultOutput))
                {
                    File.Copy(defaultOutput, tempFile, true);

                    vmArgs.Insert(0, tempFile);
                    int result = VmLauncher.Run(vmArgs.ToArray());

                    return result;
                }
                else
                {
                    Console.Error.WriteLine("Error: Compilation failed - no output file produced");
                    return 1;
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch
                {
                    // ignored
                }

                try
                {
                    string defaultOutput = Path.ChangeExtension(sourcePath, ".tbc");
                    if (File.Exists(defaultOutput) && Path.GetDirectoryName(defaultOutput) == Directory.GetCurrentDirectory())
                    {
                        File.Delete(defaultOutput);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private void PrintBuildRunUsage()
    {
        Console.WriteLine("Usage: tutel build-run <source.tl> [compiler_options] -- [vm_options]");
        Console.WriteLine();
        Console.WriteLine("Compiler options:");
        Console.WriteLine("  --help, -h           Show this help message");
        Console.WriteLine();
        Console.WriteLine("VM options (after --):");
        Console.WriteLine("  --debug, -d         Show debug information");
        Console.WriteLine("  --trace, -t         Trace instruction execution");
        Console.WriteLine("  --trace=N           Trace N instructions");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  tutel build-run program.tl");
        Console.WriteLine("  tutel build-run program.tl -- --debug");
        Console.WriteLine("  tutel build-run program.tl -- --trace=50");
    }
}