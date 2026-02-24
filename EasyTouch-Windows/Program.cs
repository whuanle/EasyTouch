using EasyTouch.Cli;
using EasyTouch.Browser;
using EasyTouch.Mcp;

if (args.Length > 0 && args[0] == "--browser-daemon")
{
    await BrowserDaemonHost.RunAsync();
}
else if (args.Length > 0 && args[0] == "--mcp")
{
    await McpServer.RunAsync();
}
else
{
    Environment.Exit(CliHost.Run(args));
}
