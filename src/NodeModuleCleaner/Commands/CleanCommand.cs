using NodeModuleCleaner.Core;
using NodeModuleCleaner.Models;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace NodeModuleCleaner.Commands;

/// <summary>
/// Clean 命令：掃描並互動式刪除 node_modules 資料夾
/// </summary>
public static class CleanCommand
{
    public static Command Create()
    {
        var command = new Command("clean", "掃描並互動式刪除 node_modules 資料夾");

        var pathArgument = BaseCommand.CreatePathArgument();
        var depthOption = BaseCommand.CreateDepthOption();
        var minSizeOption = BaseCommand.CreateMinSizeOption();

        command.Arguments.Add(pathArgument);
        command.Options.Add(depthOption);
        command.Options.Add(minSizeOption);

        command.SetAction(async parseResult =>
        {
            var path = parseResult.GetValue(pathArgument);
            var depth = parseResult.GetValue(depthOption);
            var minSize = parseResult.GetValue(minSizeOption);

            await ExecuteAsync(path!, depth, minSize);
        });

        return command;
    }

    private static async Task ExecuteAsync(string rootPath, int? maxDepth, long? minSize)
    {
        // Step 1: 掃描
        var results = await BaseCommand.ScanNodeModulesAsync(rootPath, maxDepth, minSize);

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 沒有找到 node_modules 資料夾[/]");
            return;
        }

        // Step 2: 顯示結果
        BaseCommand.DisplayResults(results);
        AnsiConsole.WriteLine();

        // Step 3: 互動式選擇
        var selectedResults = PromptForSelection(results);
        if (selectedResults.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 沒有選擇任何資料夾[/]");
            return;
        }

        // Step 4: 確認刪除
        if (!ConfirmDeletion(selectedResults))
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 取消刪除操作[/]");
            return;
        }

        // Step 5: 執行刪除
        await PerformDeletionAsync(selectedResults);
    }

    private static List<ScanResult> PromptForSelection(List<ScanResult> results)
    {
        var choices = results
            .OrderByDescending(r => r.SizeInBytes)
            .Select(r => $"{r.Path} ({BaseCommand.FormatSize(r.SizeInBytes)})")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[yellow]選擇要刪除的資料夾 (Space 切換, Enter 確認):[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](上下移動查看更多)[/]")
                .InstructionsText("[grey](使用 Space 鍵選擇, Enter 確認)[/]")
                .AddChoices(choices)
        );

        return results
            .Where(r => selected.Any(s => s.StartsWith(r.Path)))
            .ToList();
    }

    private static bool ConfirmDeletion(List<ScanResult> selectedResults)
    {
        var totalSizeToDelete = selectedResults.Sum(r => r.SizeInBytes);

        return AnsiConsole.Confirm(
            $"[red]即將刪除 {selectedResults.Count} 個資料夾 ({BaseCommand.FormatSize(totalSizeToDelete)})。確定要繼續嗎？[/]",
            false
        );
    }

    private static async Task PerformDeletionAsync(List<ScanResult> selectedResults)
    {
        var cleaner = new DirectoryCleaner();
        int successCount = 0;
        int failCount = 0;
        long freedSpace = 0;

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]刪除中...[/]", maxValue: selectedResults.Count);

                foreach (var result in selectedResults)
                {
                    var dir = new DirectoryInfo(result.Path);
                    var success = cleaner.DeleteDirectory(dir, out var error);

                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] 已刪除: {result.Path}");
                        successCount++;
                        freedSpace += result.SizeInBytes;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] 刪除失敗: {result.Path}");
                        AnsiConsole.MarkupLine($"[dim]  {error}[/]");
                        failCount++;
                    }

                    task.Increment(1);
                    await Task.Delay(50);
                }
            });

        DisplayDeletionSummary(successCount, failCount, freedSpace);
    }

    private static void DisplayDeletionSummary(int successCount, int failCount, long freedSpace)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(
            new Markup(
                $"[green]成功刪除:[/] {successCount} 個資料夾\n" +
                $"[red]失敗:[/] {failCount} 個資料夾\n" +
                $"[bold]釋放空間:[/] {BaseCommand.FormatSize(freedSpace)}"
            )
        )
        {
            Header = new PanelHeader("[bold]刪除結果[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }
}

