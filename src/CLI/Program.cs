using Tutel.CLI.CommandHandlers;

var helpHandler = new HelpHandler();

helpHandler
    .AddNext(new CompileHandler())
    .AddNext(new RunHandler())
    .AddNext(new BuildRunHandler());

helpHandler.Handle(args);