using ArtifactCleaner.Commands;
using System.CommandLine;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var rootCommand = new RootCommand("Artifact Cleaner - 快速清理專案中的建置產物資料夾（如 node_modules、bin、obj）");

rootCommand.Subcommands.Add(ScanCommand.Create());
rootCommand.Subcommands.Add(CleanCommand.Create());
rootCommand.Subcommands.Add(ConfigCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
