using NodeModuleCleaner.Commands;
using System.CommandLine;

var rootCommand = new RootCommand("Node Modules Cleaner - 快速清理 node_modules 資料夾");

rootCommand.Subcommands.Add(ScanCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
