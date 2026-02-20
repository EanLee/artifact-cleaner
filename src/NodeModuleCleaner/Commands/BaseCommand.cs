using NodeModuleCleaner.Core;
using NodeModuleCleaner.Models;
using NodeModuleCleaner.Services;
using Spectre.Console;
using System.CommandLine;

namespace NodeModuleCleaner.Commands;

/// <summary>
/// 命令基類：封裝共用的掃描和顯示邏輯
/// </summary>
public static class BaseCommand
{
    public static Argument<string> CreatePathArgument()
    {
        return new Argument<string>("path")
        {
            Description = "要掃描的根目錄路徑"
        };
    }

    public static Option<int?> CreateDepthOption()
    {
        return new Option<int?>("--depth")
        {
            Description = "限制掃描深度"
        };
    }

    public static Option<long?> CreateMinSizeOption()
    {
        return new Option<long?>("--min-size")
        {
            Description = "只顯示大於指定大小的資料夾（bytes）"
        };
    }

    public static Option<string[]> CreateFolderOption()
    {
        return new Option<string[]>("--folder")
        {
            Description = "臨時覆蓋目標資料夾名稱（不影響 config）",
            Arity = ArgumentArity.OneOrMore,
        };
    }

    /// <summary>
    /// 執行掃描並回傳結果（平行化版本）
    /// </summary>
    public static async Task<List<ScanResult>> ScanNodeModulesAsync(
        string rootPath,
        int? maxDepth,
        long? minSize,
        IEnumerable<string>? targets = null)
    {
        var scanner = new NodeModulesScanner();
        var calculator = new SizeCalculator();

        // 若未指定 targets（null 或空陣列），從 config 讀取
        var effectiveTargets = (targets != null && targets.Any())
            ? targets.ToList()
            : new ConfigService().Load().Targets;
        var targetLabel = string.Join(", ", effectiveTargets);

        return await AnsiConsole.Status()
            .StartAsync($"掃描 [[{Markup.Escape(targetLabel)}]] 資料夾中...", ctx =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var directories = scanner.ScanDirectory(rootPath, maxDepth, effectiveTargets).ToList();

                        // 平行計算所有 node_modules 的大小
                        var results = directories
                            .AsParallel()
                            .WithDegreeOfParallelism(Environment.ProcessorCount)
                            .Select(dir =>
                            {
                                var size = calculator.CalculateSize(dir);
                                AnsiConsole.MarkupLine($"[dim]找到: {dir.FullName} ({FormatSize(size)})[/]");
                                return new ScanResult(dir.FullName, size, dir.LastWriteTime);
                            })
                            .Where(result => !minSize.HasValue || result.SizeInBytes >= minSize.Value)
                            .ToList();

                        return results;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗ 錯誤: {ex.Message}[/]");
                        Environment.Exit(1);
                        return new List<ScanResult>();
                    }
                });
            });
    }

    /// <summary>
    /// 顯示掃描結果表格
    /// </summary>
    public static void DisplayResults(List<ScanResult> results)
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

    /// <summary>
    /// 格式化檔案大小
    /// </summary>
    public static string FormatSize(long bytes)
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
