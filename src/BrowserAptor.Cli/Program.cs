using BrowserAptor.CLI;

int exitCode = 0;
CliHandler.TryHandle(args, out exitCode);
return exitCode;
