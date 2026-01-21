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

        bool debug = false;
        bool trace = false;
        int traceLimit = 100;
        string? filePath = null;
        bool showStats = false;
        bool enableJit = true;

        foreach (string arg in args)
        {
            if (arg is "--help" or "-h")
            {
                PrintUsage();
                return 0;
            }

            if (arg is "--version" or "-v")
            {
                PrintVersion();
                return 0;
            }

            if (arg is "--debug" or "-d")
            {
                debug = true;
                continue;
            }

            if (arg is "--trace" or "-t")
            {
                trace = true;
                continue;
            }

            if (arg.StartsWith("--trace="))
            {
                trace = true;
                if (int.TryParse(arg.AsSpan(8), out int limit))
                {
                    traceLimit = limit;
                }

                continue;
            }

            if (arg == "--stats")
            {
                showStats = true;
                continue;
            }

            if (arg == "--jit=off")
            {
                enableJit = false;
                continue;
            }

            filePath = arg;
        }

        if (filePath == null)
        {
            PrintUsage();
            return 1;
        }

        return RunFile(filePath, debug, trace, traceLimit, showStats, enableJit);
    }

    private static int RunFile(
        string filePath,
        bool debug = false,
        bool trace = false,
        int traceLimit = 100,
        bool showStats = false,
        bool enableJit = true)
    {
        try
        {
            var vm = new TutelVm();
            vm.Load(filePath, enableJit);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            long result = vm.Run(trace, traceLimit);
            sw.Stop();

            Console.WriteLine(result);

            if (debug)
            {
                PrintHeapArrays(vm);
            }

            if (showStats)
            {
                PrintExecutionStats(sw.Elapsed);
            }

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

    private static void PrintExecutionStats(TimeSpan elapsed)
    {
        Console.WriteLine();
        Console.WriteLine("=== Debug: Execution Stats ===");
        if (elapsed.TotalSeconds >= 1)
        {
            Console.WriteLine($"Time: {elapsed.TotalSeconds:F3} s");
        }
        else
        {
            Console.WriteLine($"Time: {elapsed.TotalMilliseconds:F2} ms");
        }
    }

    private static void PrintHeapArrays(TutelVm vm)
    {
        Console.WriteLine();
        Console.WriteLine("=== Debug: Heap Arrays ===");
        Dictionary<long, long[]> arrays = vm.GetHeapArrays();
        if (arrays.Count == 0)
        {
            Console.WriteLine("(no arrays)");
            return;
        }

        foreach (KeyValuePair<long, long[]> kvp in arrays)
        {
            Console.WriteLine($"Array @{kvp.Key}: [{string.Join(", ", kvp.Value)}]");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Tutel Virtual Machine");
        Console.WriteLine();
        Console.WriteLine("Usage: tutel <file.tbc> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h       Show this help message");
        Console.WriteLine("  --version, -v    Show version information");
        Console.WriteLine("  --debug, -d      Show heap arrays after execution");
        Console.WriteLine("  --trace, -t      Trace instruction execution (100 max)");
        Console.WriteLine("  --trace=N        Trace N instructions (0 = unlimited)");
        Console.WriteLine("  --stats          Show statistics after execution");
        Console.WriteLine("  --jit=on/off     Enables or disables using JIT during program execution");
    }

    private static void PrintVersion()
    {
        Console.WriteLine($"Tutel VM v{BytecodeFormat.Version}");
    }
}