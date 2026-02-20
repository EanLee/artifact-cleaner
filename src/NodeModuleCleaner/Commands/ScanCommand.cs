using NodeModuleCleaner.Core;
using NodeModuleCleaner.Models;
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
        var results = new List<ScanResult>();

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

                            // 套用最小大小過濾
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

        // 顯示結果表格
        DisplayResults(results);
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

        // 顯示總計
        AnsiConsole.WriteLine();
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
