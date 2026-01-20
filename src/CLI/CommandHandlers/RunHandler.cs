using Tutel.VirtualMachine.CLI;

namespace Tutel.CLI.CommandHandlers;

public class RunHandler : CommandHandlerBase
{
    protected override bool CanHandle(string[] args)
    {
        return args.Length > 0 && (args[0] == "run" || args[0] == "r");
    }

    protected override int Process(string[] args)
    {
        string[] vmArgs = args.Skip(1).ToArray();
        return VmLauncher.Run(vmArgs);
    }
}