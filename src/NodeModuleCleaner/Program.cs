using NodeModuleCleaner.Commands;
using System.CommandLine;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var rootCommand = new RootCommand("Node Modules Cleaner - 快速清理 node_modules 資料夾");

rootCommand.Subcommands.Add(ScanCommand.Create());
rootCommand.Subcommands.Add(CleanCommand.Create());
rootCommand.Subcommands.Add(ConfigCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
