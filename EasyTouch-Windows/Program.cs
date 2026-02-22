using EasyTouch.Cli;
using EasyTouch.Mcp;

if (args.Length > 0 && args[0] == "--mcp")
{
    await McpServer.RunAsync();
}
else
{
    Environment.Exit(CliHost.Run(args));
}
