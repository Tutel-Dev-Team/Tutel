namespace Tutel.CLI.CommandHandlers;

public interface ICommandHandler
{
    ICommandHandler AddNext(ICommandHandler handler);

    int Handle(string[] args);
}