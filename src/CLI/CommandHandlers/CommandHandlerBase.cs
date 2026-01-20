namespace Tutel.CLI.CommandHandlers;

public abstract class CommandHandlerBase : ICommandHandler
{
    protected ICommandHandler? Next { get; private set; }

    public ICommandHandler AddNext(ICommandHandler handler)
    {
        if (Next != null)
        {
            Next.AddNext(handler);
        }
        else
        {
            Next = handler;
        }

        return this;
    }

    public int Handle(string[] args)
    {
        if (CanHandle(args))
        {
            return Process(args);
        }
        else if (Next != null)
        {
            return Next.Handle(args);
        }
        else
        {
            Console.Error.WriteLine("Error: Unknown command");
            PrintUsage();
            return 1;
        }
    }

    protected virtual void PrintUsage()
    {
        Console.WriteLine("Usage: tutel <command> [options]");
        Console.WriteLine("Commands: compile, run, build-run");
        Console.WriteLine("Use 'tutel <command> --help' for command-specific help");
    }

    protected abstract bool CanHandle(string[] args);

    protected abstract int Process(string[] args);
}