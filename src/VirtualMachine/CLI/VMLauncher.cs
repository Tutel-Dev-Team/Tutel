// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

using Tutel.VirtualMachine.Core;

namespace Tutel.VirtualMachine.CLI;

/// <summary>
/// CLI launcher for running Tutel bytecode files.
/// </summary>
public static class VmLauncher
{
    /// <summary>
    /// Runs the VM with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string command = args[0];

        if (command is "--help" or "-h")
        {
            PrintUsage();
            return 0;
        }

        if (command is "--version" or "-v")
        {
            PrintVersion();
            return 0;
        }

        // Treat the argument as a file path
        return RunFile(command);
    }

    private static int RunFile(string filePath)
    {
        try
        {
            var vm = new TutelVm();
            vm.Load(filePath);
            long result = vm.Run();

            Console.WriteLine(result);
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (DivideByZeroException ex)
        {
            Console.Error.WriteLine($"Runtime error: {ex.Message}");
            return 1;
        }
        catch (IndexOutOfRangeException ex)
        {
            Console.Error.WriteLine($"Runtime error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Tutel Virtual Machine");
        Console.WriteLine();
        Console.WriteLine("Usage: tutel <file.tbc>");
        Console.WriteLine("       tutel --help");
        Console.WriteLine("       tutel --version");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <file.tbc>    Path to bytecode file to execute");
        Console.WriteLine("  --help, -h    Show this help message");
        Console.WriteLine("  --version, -v Show version information");
    }

    private static void PrintVersion()
    {
        Console.WriteLine($"Tutel VM v{BytecodeFormat.Version}");
    }
}
