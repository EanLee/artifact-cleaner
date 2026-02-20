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

        var pathArgument = new Argument<string>("path")
        {
            Description = "要掃描的根目錄路徑"
        };

        var depthOption = new Option<int?>("--depth")
        {
            Description = "限制掃描深度"
        };

        var minSizeOption = new Option<long?>("--min-size")
        {
            Description = "只顯示大於指定大小的資料夾（bytes）"
        };

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
        var scanner = new NodeModulesScanner();
        var calculator = new SizeCalculator();
        var cleaner = new DirectoryCleaner();
        var results = new List<ScanResult>();

        // Step 1: 掃描
        await AnsiConsole.Status()
            .StartAsync("掃描 node_modules 資料夾中...", ctx =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        foreach (var dir in scanner.ScanDirectory(rootPath, maxDepth))
                        {
                            var size = calculator.CalculateSize(dir);
                            var lastModified = dir.LastWriteTime;
                            var result = new ScanResult(dir.FullName, size, lastModified);

                            if (!minSize.HasValue || size >= minSize.Value)
                            {
                                results.Add(result);
                                AnsiConsole.MarkupLine($"[dim]找到: {dir.FullName}[/]");
                            }
                        }
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗ 錯誤: {ex.Message}[/]");
                        Environment.Exit(1);
                    }
                });
            });

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 沒有找到 node_modules 資料夾[/]");
            return;
        }

        // Step 2: 顯示結果
        DisplayResults(results);
        AnsiConsole.WriteLine();

        // Step 3: 互動式選擇
        var choices = results
            .OrderByDescending(r => r.SizeInBytes)
            .Select(r => $"{r.Path} ({FormatSize(r.SizeInBytes)})")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[yellow]選擇要刪除的資料夾 (Space 切換, Enter 確認):[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](上下移動查看更多)[/]")
                .InstructionsText("[grey](使用 Space 鍵選擇, Enter 確認)[/]")
                .AddChoices(choices)
        );

        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 沒有選擇任何資料夾[/]");
            return;
        }

        // Step 4: 計算要刪除的總大小
        var selectedResults = results
            .Where(r => selected.Any(s => s.StartsWith(r.Path)))
            .ToList();

        var totalSizeToDelete = selectedResults.Sum(r => r.SizeInBytes);

        // Step 5: 確認刪除
        var confirm = AnsiConsole.Confirm(
            $"[red]即將刪除 {selectedResults.Count} 個資料夾 ({FormatSize(totalSizeToDelete)})。確定要繼續嗎？[/]",
            false
        );

        if (!confirm)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 取消刪除操作[/]");
            return;
        }

        // Step 6: 執行刪除
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
                    await Task.Delay(50); // 稍微延遲讓使用者看到進度
                }
            });

        // Step 7: 顯示結果摘要
        AnsiConsole.WriteLine();
        var panel = new Panel(
            new Markup(
                $"[green]成功刪除:[/] {successCount} 個資料夾\n" +
                $"[red]失敗:[/] {failCount} 個資料夾\n" +
                $"[bold]釋放空間:[/] {FormatSize(freedSpace)}"
            )
        )
        {
            Header = new PanelHeader("[bold]刪除結果[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

    private static void DisplayResults(List<ScanResult> results)
    {
        var table = new Table();
        table.AddColumn("路徑");
        table.AddColumn(new TableColumn("大小").RightAligned());
        table.AddColumn("最後修改時間");

        long totalSize = 0;

        foreach (var result in results.OrderByDescending(r => r.SizeInBytes))
        {
            table.AddRow(
                result.Path,
                FormatSize(result.SizeInBytes),
                result.LastModified.ToString("yyyy-MM-dd")
            );
            totalSize += result.SizeInBytes;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[bold]總計:[/] {results.Count} 個資料夾, {FormatSize(totalSize)}");
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
