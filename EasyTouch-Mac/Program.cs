using EasyTouch.Mcp;

if (args.Length > 0 && args[0] == "--mcp")
{
    McpHost.RunStdio();
}
else
{
    Environment.Exit(EasyTouch.Cli.CliHost.Run(args));
}
