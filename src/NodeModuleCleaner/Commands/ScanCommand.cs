using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace NodeModuleCleaner.Commands;

/// <summary>
/// Scan 命令：掃描並顯示 node_modules 資料夾
/// </summary>
public static class ScanCommand
{
    public static Command Create()
    {
        var command = new Command("scan", "掃描指定目錄下的所有 node_modules 資料夾");

        var pathArgument = BaseCommand.CreatePathArgument();
        var depthOption = BaseCommand.CreateDepthOption();
        var minSizeOption = BaseCommand.CreateMinSizeOption();
        var folderOption = BaseCommand.CreateFolderOption();

        command.Arguments.Add(pathArgument);
        command.Options.Add(depthOption);
        command.Options.Add(minSizeOption);
        command.Options.Add(folderOption);

        command.SetAction(async parseResult =>
        {
            var path = parseResult.GetValue(pathArgument);
            var depth = parseResult.GetValue(depthOption);
            var minSize = parseResult.GetValue(minSizeOption);
            var folders = parseResult.GetValue(folderOption);

            await ExecuteAsync(path!, depth, minSize, folders);
        });

        return command;
    }

    private static async Task ExecuteAsync(string rootPath, int? maxDepth, long? minSize, string[]? folders)
    {
        var results = await BaseCommand.ScanNodeModulesAsync(rootPath, maxDepth, minSize, folders);

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 沒有找到符合的資料夾[/]");
            return;
        }

        BaseCommand.DisplayResults(results);
    }
}
