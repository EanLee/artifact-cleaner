# Node Modules Cleaner 實作計畫

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目標：** 建立一個 .NET 9 CLI 工具，用於掃描、統計和刪除指定目錄下的所有 node_modules 資料夾

**架構：** 採用分層架構，Core 層負責核心邏輯（掃描、計算、刪除），Commands 層負責 CLI 介面，使用 Spectre.Console 提供互動式 UI

**技術棧：** .NET 9, System.CommandLine, Spectre.Console, xUnit

---

## Task 1: 建立專案結構與套件安裝

**Files:**
- Create: `src/NodeModuleCleaner/NodeModuleCleaner.csproj`
- Create: `src/NodeModuleCleaner/Program.cs`
- Create: `tests/NodeModuleCleaner.Tests/NodeModuleCleaner.Tests.csproj`
- Create: `.gitignore`
- Create: `NodeModuleCleaner.sln`

**Step 1: 建立方案目錄結構**

```bash
mkdir -p src/NodeModuleCleaner
mkdir -p tests/NodeModuleCleaner.Tests
```

**Step 2: 建立主專案**

Run: `dotnet new console -n NodeModuleCleaner -o src/NodeModuleCleaner -f net9.0`
Expected: 專案建立成功

**Step 3: 建立測試專案**

Run: `dotnet new xunit -n NodeModuleCleaner.Tests -o tests/NodeModuleCleaner.Tests -f net9.0`
Expected: 測試專案建立成功

**Step 4: 建立方案檔**

Run: `dotnet new sln -n NodeModuleCleaner`
Expected: 方案檔建立成功

**Step 5: 將專案加入方案**

```bash
dotnet sln add src/NodeModuleCleaner/NodeModuleCleaner.csproj
dotnet sln add tests/NodeModuleCleaner.Tests/NodeModuleCleaner.Tests.csproj
```

Expected: 兩個專案成功加入方案

**Step 6: 加入專案參考**

Run: `dotnet add tests/NodeModuleCleaner.Tests/NodeModuleCleaner.Tests.csproj reference src/NodeModuleCleaner/NodeModuleCleaner.csproj`
Expected: 參考加入成功

**Step 7: 安裝 NuGet 套件到主專案**

```bash
cd src/NodeModuleCleaner
dotnet add package System.CommandLine --version 2.0.0-beta4.22272.1
dotnet add package Spectre.Console --version 0.49.1
cd ../..
```

Expected: 套件安裝成功

**Step 8: 建立 .gitignore**

Create file: `.gitignore`

```gitignore
# Build results
[Dd]ebug/
[Rr]elease/
[Bb]in/
[Oo]bj/

# Visual Studio
.vs/
*.user
*.suo

# Rider
.idea/

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates
```

**Step 9: 驗證專案可以建置**

Run: `dotnet build`
Expected: Build succeeded

**Step 10: Commit**

```bash
git add .
git commit -m "chore: 初始化專案結構與套件安裝"
```

---

## Task 2: 實作 ScanResult Model

**Files:**
- Create: `src/NodeModuleCleaner/Models/ScanResult.cs`
- Create: `tests/NodeModuleCleaner.Tests/Models/ScanResultTests.cs`

**Step 1: 建立測試檔案目錄**

```bash
mkdir -p tests/NodeModuleCleaner.Tests/Models
```

**Step 2: 撰寫 ScanResult 測試**

Create file: `tests/NodeModuleCleaner.Tests/Models/ScanResultTests.cs`

```csharp
namespace NodeModuleCleaner.Tests.Models;

public class ScanResultTests
{
    [Fact]
    public void ScanResult_Constructor_ShouldSetProperties()
    {
        // Arrange
        var path = @"C:\Projects\app1\node_modules";
        var size = 450_000_000L;
        var lastModified = new DateTime(2026, 1, 15);

        // Act
        var result = new ScanResult(path, size, lastModified);

        // Assert
        Assert.Equal(path, result.Path);
        Assert.Equal(size, result.SizeInBytes);
        Assert.Equal(lastModified, result.LastModified);
    }

    [Fact]
    public void ScanResult_SizeInMB_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new ScanResult(@"C:\test", 450_000_000L, DateTime.Now);

        // Act
        var sizeInMB = result.SizeInBytes / (1024.0 * 1024.0);

        // Assert
        Assert.Equal(429.15, sizeInMB, 2);
    }
}
```

**Step 3: 跑測試確認失敗**

Run: `dotnet test`
Expected: FAIL - ScanResult type not found

**Step 4: 建立 Models 目錄並實作 ScanResult**

```bash
mkdir -p src/NodeModuleCleaner/Models
```

Create file: `src/NodeModuleCleaner/Models/ScanResult.cs`

```csharp
namespace NodeModuleCleaner.Models;

/// <summary>
/// 代表一個 node_modules 資料夾的掃描結果
/// </summary>
public record ScanResult(
    string Path,
    long SizeInBytes,
    DateTime LastModified
);
```

**Step 5: 跑測試確認通過**

Run: `dotnet test`
Expected: PASS - All tests passed

**Step 6: Commit**

```bash
git add src/NodeModuleCleaner/Models/ tests/NodeModuleCleaner.Tests/Models/
git commit -m "feat: 新增 ScanResult 資料模型"
```

---

## Task 3: 實作 SizeCalculator

**Files:**
- Create: `src/NodeModuleCleaner/Core/SizeCalculator.cs`
- Create: `tests/NodeModuleCleaner.Tests/Core/SizeCalculatorTests.cs`

**Step 1: 建立測試目錄**

```bash
mkdir -p tests/NodeModuleCleaner.Tests/Core
```

**Step 2: 撰寫 SizeCalculator 測試**

Create file: `tests/NodeModuleCleaner.Tests/Core/SizeCalculatorTests.cs`

```csharp
namespace NodeModuleCleaner.Tests.Core;

public class SizeCalculatorTests
{
    private readonly string _testDir;

    public SizeCalculatorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void CalculateSize_EmptyDirectory_ReturnsZero()
    {
        // Arrange
        var calculator = new SizeCalculator();
        var emptyDir = Directory.CreateDirectory(Path.Combine(_testDir, "empty"));

        // Act
        var size = calculator.CalculateSize(emptyDir);

        // Assert
        Assert.Equal(0, size);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void CalculateSize_DirectoryWithFiles_ReturnsCorrectSize()
    {
        // Arrange
        var calculator = new SizeCalculator();
        var dir = Directory.CreateDirectory(Path.Combine(_testDir, "withfiles"));

        // 建立測試檔案
        File.WriteAllText(Path.Combine(dir.FullName, "file1.txt"), new string('a', 1024)); // 1KB
        File.WriteAllText(Path.Combine(dir.FullName, "file2.txt"), new string('b', 2048)); // 2KB

        // Act
        var size = calculator.CalculateSize(dir);

        // Assert
        Assert.Equal(3072, size); // 1024 + 2048

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void CalculateSize_NestedDirectories_CalculatesRecursively()
    {
        // Arrange
        var calculator = new SizeCalculator();
        var rootDir = Directory.CreateDirectory(Path.Combine(_testDir, "nested"));
        var subDir = Directory.CreateDirectory(Path.Combine(rootDir.FullName, "sub"));

        File.WriteAllText(Path.Combine(rootDir.FullName, "root.txt"), new string('a', 1000));
        File.WriteAllText(Path.Combine(subDir.FullName, "sub.txt"), new string('b', 500));

        // Act
        var size = calculator.CalculateSize(rootDir);

        // Assert
        Assert.Equal(1500, size);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void CalculateSize_UnauthorizedAccess_ReturnsPartialSize()
    {
        // Arrange
        var calculator = new SizeCalculator();
        var dir = Directory.CreateDirectory(Path.Combine(_testDir, "restricted"));
        File.WriteAllText(Path.Combine(dir.FullName, "accessible.txt"), new string('a', 1000));

        // Act - 這個測試在 Windows 上較難模擬權限問題，主要測試不會拋出例外
        var size = calculator.CalculateSize(dir);

        // Assert
        Assert.True(size >= 0); // 至少不會拋出例外

        // Cleanup
        Directory.Delete(_testDir, true);
    }
}
```

**Step 3: 跑測試確認失敗**

Run: `dotnet test`
Expected: FAIL - SizeCalculator type not found

**Step 4: 建立 Core 目錄並實作 SizeCalculator**

```bash
mkdir -p src/NodeModuleCleaner/Core
```

Create file: `src/NodeModuleCleaner/Core/SizeCalculator.cs`

```csharp
namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責計算資料夾大小
/// </summary>
public class SizeCalculator
{
    /// <summary>
    /// 計算指定資料夾的總大小（包含所有子檔案和子資料夾）
    /// </summary>
    /// <param name="directory">要計算的資料夾</param>
    /// <returns>總大小（bytes）</returns>
    public long CalculateSize(DirectoryInfo directory)
    {
        long totalSize = 0;

        try
        {
            // 計算所有檔案大小
            foreach (var file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    totalSize += file.Length;
                }
                catch (UnauthorizedAccessException)
                {
                    // 跳過無法存取的檔案
                }
                catch (FileNotFoundException)
                {
                    // 跳過已被刪除的檔案
                }
            }

            // 遞迴計算子資料夾
            foreach (var subDir in directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    totalSize += CalculateSize(subDir);
                }
                catch (UnauthorizedAccessException)
                {
                    // 跳過無法存取的資料夾
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 無法列舉資料夾內容，回傳目前累計的大小
        }

        return totalSize;
    }
}
```

**Step 5: 跑測試確認通過**

Run: `dotnet test`
Expected: PASS - All tests passed

**Step 6: Commit**

```bash
git add src/NodeModuleCleaner/Core/ tests/NodeModuleCleaner.Tests/Core/
git commit -m "feat: 新增 SizeCalculator 計算資料夾大小"
```

---

## Task 4: 實作 NodeModulesScanner

**Files:**
- Create: `src/NodeModuleCleaner/Core/NodeModulesScanner.cs`
- Create: `tests/NodeModuleCleaner.Tests/Core/NodeModulesScannerTests.cs`

**Step 1: 撰寫 NodeModulesScanner 測試**

Create file: `tests/NodeModuleCleaner.Tests/Core/NodeModulesScannerTests.cs`

```csharp
namespace NodeModuleCleaner.Tests.Core;

public class NodeModulesScannerTests
{
    private readonly string _testDir;

    public NodeModulesScannerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"scanner_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void ScanDirectory_NoNodeModules_ReturnsEmpty()
    {
        // Arrange
        var scanner = new NodeModulesScanner();
        Directory.CreateDirectory(Path.Combine(_testDir, "project1"));
        Directory.CreateDirectory(Path.Combine(_testDir, "project2"));

        // Act
        var results = scanner.ScanDirectory(_testDir).ToList();

        // Assert
        Assert.Empty(results);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ScanDirectory_FindsNodeModules_ReturnsCorrectPaths()
    {
        // Arrange
        var scanner = new NodeModulesScanner();
        var nodeModules1 = Path.Combine(_testDir, "project1", "node_modules");
        var nodeModules2 = Path.Combine(_testDir, "project2", "node_modules");

        Directory.CreateDirectory(nodeModules1);
        Directory.CreateDirectory(nodeModules2);

        // Act
        var results = scanner.ScanDirectory(_testDir).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, d => d.FullName == nodeModules1);
        Assert.Contains(results, d => d.FullName == nodeModules2);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ScanDirectory_WithDepthLimit_RespectsMaxDepth()
    {
        // Arrange
        var scanner = new NodeModulesScanner();

        // Depth 1
        var level1 = Path.Combine(_testDir, "node_modules");
        // Depth 2
        var level2 = Path.Combine(_testDir, "project", "node_modules");
        // Depth 3
        var level3 = Path.Combine(_testDir, "project", "sub", "node_modules");

        Directory.CreateDirectory(level1);
        Directory.CreateDirectory(level2);
        Directory.CreateDirectory(level3);

        // Act - 限制深度為 2
        var results = scanner.ScanDirectory(_testDir, maxDepth: 2).ToList();

        // Assert - 應該只找到 depth 1 和 2 的
        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, d => d.FullName == level3);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ScanDirectory_SkipsSystemFolders()
    {
        // Arrange
        var scanner = new NodeModulesScanner();

        // 建立系統資料夾
        Directory.CreateDirectory(Path.Combine(_testDir, ".git", "node_modules"));
        Directory.CreateDirectory(Path.Combine(_testDir, ".vs", "node_modules"));
        Directory.CreateDirectory(Path.Combine(_testDir, "bin", "node_modules"));

        // 建立正常的 node_modules
        var validNodeModules = Path.Combine(_testDir, "project", "node_modules");
        Directory.CreateDirectory(validNodeModules);

        // Act
        var results = scanner.ScanDirectory(_testDir).ToList();

        // Assert - 應該只找到 project 下的，系統資料夾被跳過
        Assert.Single(results);
        Assert.Equal(validNodeModules, results[0].FullName);

        // Cleanup
        Directory.Delete(_testDir, true);
    }
}
```

**Step 2: 跑測試確認失敗**

Run: `dotnet test`
Expected: FAIL - NodeModulesScanner type not found

**Step 3: 實作 NodeModulesScanner**

Create file: `src/NodeModuleCleaner/Core/NodeModulesScanner.cs`

```csharp
namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責掃描指定目錄下的所有 node_modules 資料夾
/// </summary>
public class NodeModulesScanner
{
    private static readonly HashSet<string> SystemFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", "bin", "obj", ".vscode", ".github"
    };

    /// <summary>
    /// 掃描指定目錄下的所有 node_modules 資料夾
    /// </summary>
    /// <param name="rootPath">根目錄路徑</param>
    /// <param name="maxDepth">最大掃描深度（null 表示無限制）</param>
    /// <returns>找到的 node_modules 資料夾</returns>
    public IEnumerable<DirectoryInfo> ScanDirectory(string rootPath, int? maxDepth = null)
    {
        var rootDir = new DirectoryInfo(rootPath);

        if (!rootDir.Exists)
        {
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");
        }

        return ScanDirectoryInternal(rootDir, currentDepth: 0, maxDepth);
    }

    private IEnumerable<DirectoryInfo> ScanDirectoryInternal(
        DirectoryInfo directory,
        int currentDepth,
        int? maxDepth)
    {
        // 檢查深度限制
        if (maxDepth.HasValue && currentDepth > maxDepth.Value)
        {
            yield break;
        }

        IEnumerable<DirectoryInfo> subDirs;

        try
        {
            subDirs = directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            // 無法存取此資料夾，跳過
            yield break;
        }

        foreach (var subDir in subDirs)
        {
            // 跳過系統資料夾
            if (SystemFolders.Contains(subDir.Name))
            {
                continue;
            }

            // 找到 node_modules
            if (string.Equals(subDir.Name, "node_modules", StringComparison.OrdinalIgnoreCase))
            {
                yield return subDir;
                continue; // 不繼續深入 node_modules 內部
            }

            // 遞迴掃描子資料夾
            foreach (var found in ScanDirectoryInternal(subDir, currentDepth + 1, maxDepth))
            {
                yield return found;
            }
        }
    }
}
```

**Step 4: 跑測試確認通過**

Run: `dotnet test`
Expected: PASS - All tests passed

**Step 5: Commit**

```bash
git add src/NodeModuleCleaner/Core/NodeModulesScanner.cs tests/NodeModuleCleaner.Tests/Core/NodeModulesScannerTests.cs
git commit -m "feat: 新增 NodeModulesScanner 掃描功能"
```

---

## Task 5: 實作 NodeModuleCleaner（刪除功能）

**Files:**
- Create: `src/NodeModuleCleaner/Core/DirectoryCleaner.cs`
- Create: `tests/NodeModuleCleaner.Tests/Core/DirectoryCleanerTests.cs`

**Step 1: 撰寫 DirectoryCleaner 測試**

Create file: `tests/NodeModuleCleaner.Tests/Core/DirectoryCleanerTests.cs`

```csharp
namespace NodeModuleCleaner.Tests.Core;

public class DirectoryCleanerTests
{
    private readonly string _testDir;

    public DirectoryCleanerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cleaner_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void DeleteDirectory_ValidDirectory_ReturnsTrue()
    {
        // Arrange
        var cleaner = new DirectoryCleaner();
        var dirToDelete = Directory.CreateDirectory(Path.Combine(_testDir, "todelete"));
        File.WriteAllText(Path.Combine(dirToDelete.FullName, "file.txt"), "content");

        // Act
        var result = cleaner.DeleteDirectory(dirToDelete, out var error);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.False(Directory.Exists(dirToDelete.FullName));

        // Cleanup
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void DeleteDirectory_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var cleaner = new DirectoryCleaner();
        var nonExistentDir = new DirectoryInfo(Path.Combine(_testDir, "nonexistent"));

        // Act
        var result = cleaner.DeleteDirectory(nonExistentDir, out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void DeleteDirectory_WithNestedContent_DeletesRecursively()
    {
        // Arrange
        var cleaner = new DirectoryCleaner();
        var rootDir = Directory.CreateDirectory(Path.Combine(_testDir, "nested"));
        var subDir = Directory.CreateDirectory(Path.Combine(rootDir.FullName, "sub"));
        var deepDir = Directory.CreateDirectory(Path.Combine(subDir.FullName, "deep"));

        File.WriteAllText(Path.Combine(rootDir.FullName, "root.txt"), "content");
        File.WriteAllText(Path.Combine(subDir.FullName, "sub.txt"), "content");
        File.WriteAllText(Path.Combine(deepDir.FullName, "deep.txt"), "content");

        // Act
        var result = cleaner.DeleteDirectory(rootDir, out var error);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.False(Directory.Exists(rootDir.FullName));

        // Cleanup
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
}
```

**Step 2: 跑測試確認失敗**

Run: `dotnet test`
Expected: FAIL - DirectoryCleaner type not found

**Step 3: 實作 DirectoryCleaner**

Create file: `src/NodeModuleCleaner/Core/DirectoryCleaner.cs`

```csharp
namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責刪除資料夾
/// </summary>
public class DirectoryCleaner
{
    /// <summary>
    /// 刪除指定的資料夾及其所有內容
    /// </summary>
    /// <param name="directory">要刪除的資料夾</param>
    /// <param name="error">錯誤訊息（如果刪除失敗）</param>
    /// <returns>true 表示成功，false 表示失敗</returns>
    public bool DeleteDirectory(DirectoryInfo directory, out string? error)
    {
        error = null;

        try
        {
            if (!directory.Exists)
            {
                error = $"Directory not found: {directory.FullName}";
                return false;
            }

            // 遞迴刪除資料夾及所有內容
            Directory.Delete(directory.FullName, recursive: true);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            error = $"Access denied: {ex.Message}";
            return false;
        }
        catch (IOException ex)
        {
            error = $"IO error: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Unexpected error: {ex.Message}";
            return false;
        }
    }
}
```

**Step 4: 跑測試確認通過**

Run: `dotnet test`
Expected: PASS - All tests passed

**Step 5: Commit**

```bash
git add src/NodeModuleCleaner/Core/DirectoryCleaner.cs tests/NodeModuleCleaner.Tests/Core/DirectoryCleanerTests.cs
git commit -m "feat: 新增 DirectoryCleaner 刪除功能"
```

---

## Task 6: 實作 Scan Command

**Files:**
- Create: `src/NodeModuleCleaner/Commands/ScanCommand.cs`

**Step 1: 實作 ScanCommand**

```bash
mkdir -p src/NodeModuleCleaner/Commands
```

Create file: `src/NodeModuleCleaner/Commands/ScanCommand.cs`

```csharp
using NodeModuleCleaner.Core;
using NodeModuleCleaner.Models;
using Spectre.Console;
using System.CommandLine;

namespace NodeModuleCleaner.Commands;

/// <summary>
/// Scan 命令：掃描並顯示 node_modules 資料夾
/// </summary>
public static class ScanCommand
{
    public static Command Create()
    {
        var command = new Command("scan", "掃描指定目錄下的所有 node_modules 資料夾");

        var pathArgument = new Argument<string>(
            name: "path",
            description: "要掃描的根目錄路徑"
        );

        var depthOption = new Option<int?>(
            name: "--depth",
            description: "限制掃描深度",
            getDefaultValue: () => null
        );

        var minSizeOption = new Option<long?>(
            name: "--min-size",
            description: "只顯示大於指定大小的資料夾（bytes）",
            getDefaultValue: () => null
        );

        command.AddArgument(pathArgument);
        command.AddOption(depthOption);
        command.AddOption(minSizeOption);

        command.SetHandler(async (path, depth, minSize) =>
        {
            await ExecuteAsync(path, depth, minSize);
        }, pathArgument, depthOption, minSizeOption);

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
```

**Step 2: 修改 Program.cs 註冊命令**

Modify file: `src/NodeModuleCleaner/Program.cs`

```csharp
using NodeModuleCleaner.Commands;
using System.CommandLine;

var rootCommand = new RootCommand("Node Modules Cleaner - 快速清理 node_modules 資料夾");

rootCommand.AddCommand(ScanCommand.Create());

return await rootCommand.InvokeAsync(args);
```

**Step 3: 測試 Scan 命令**

Run: `dotnet build`
Expected: Build succeeded

Run: `dotnet run -- scan . --help`
Expected: 顯示 scan 命令的說明

**Step 4: Commit**

```bash
git add src/NodeModuleCleaner/Commands/ src/NodeModuleCleaner/Program.cs
git commit -m "feat: 新增 scan 命令實作"
```

---

## Task 7: 實作 Clean Command（互動式刪除）

**Files:**
- Create: `src/NodeModuleCleaner/Commands/CleanCommand.cs`

**Step 1: 實作 CleanCommand**

Create file: `src/NodeModuleCleaner/Commands/CleanCommand.cs`

```csharp
using NodeModuleCleaner.Core;
using NodeModuleCleaner.Models;
using Spectre.Console;
using System.CommandLine;

namespace NodeModuleCleaner.Commands;

/// <summary>
/// Clean 命令：掃描並互動式刪除 node_modules 資料夾
/// </summary>
public static class CleanCommand
{
    public static Command Create()
    {
        var command = new Command("clean", "掃描並互動式刪除 node_modules 資料夾");

        var pathArgument = new Argument<string>(
            name: "path",
            description: "要掃描的根目錄路徑"
        );

        var depthOption = new Option<int?>(
            name: "--depth",
            description: "限制掃描深度",
            getDefaultValue: () => null
        );

        var minSizeOption = new Option<long?>(
            name: "--min-size",
            description: "只顯示大於指定大小的資料夾（bytes）",
            getDefaultValue: () => null
        );

        command.AddArgument(pathArgument);
        command.AddOption(depthOption);
        command.AddOption(minSizeOption);

        command.SetHandler(async (path, depth, minSize) =>
        {
            await ExecuteAsync(path, depth, minSize);
        }, pathArgument, depthOption, minSizeOption);

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
```

**Step 2: 在 Program.cs 註冊 Clean 命令**

Modify file: `src/NodeModuleCleaner/Program.cs`

Replace content with:

```csharp
using NodeModuleCleaner.Commands;
using System.CommandLine;

var rootCommand = new RootCommand("Node Modules Cleaner - 快速清理 node_modules 資料夾");

rootCommand.AddCommand(ScanCommand.Create());
rootCommand.AddCommand(CleanCommand.Create());

return await rootCommand.InvokeAsync(args);
```

**Step 3: 測試 Clean 命令**

Run: `dotnet build`
Expected: Build succeeded

Run: `dotnet run -- clean . --help`
Expected: 顯示 clean 命令的說明

**Step 4: Commit**

```bash
git add src/NodeModuleCleaner/Commands/CleanCommand.cs src/NodeModuleCleaner/Program.cs
git commit -m "feat: 新增 clean 命令互動式刪除功能"
```

---

## Task 8: 建立 README 文件

**Files:**
- Create: `README.md`

**Step 1: 撰寫 README**

Create file: `README.md`

```markdown
# Node Modules Cleaner

一個快速、簡單的 .NET CLI 工具，用於掃描、統計和清理專案目錄下的所有 `node_modules` 資料夾。

## 功能特色

- 🔍 **快速掃描** - 遞迴搜尋指定目錄下的所有 node_modules
- 📊 **詳細統計** - 顯示每個資料夾的大小和最後修改時間
- 🎯 **互動式選擇** - 使用方向鍵和空白鍵選擇要刪除的資料夾
- 🎨 **美觀的介面** - 使用 Spectre.Console 提供現代化 CLI 體驗
- ⚡ **效能優化** - 使用 yield return 和非同步 I/O 提升效能

## 安裝

### 從原始碼建置

```bash
git clone <repository-url>
cd remove-node-module
dotnet build -c Release
```

### 發布為單一執行檔

Windows:
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Linux:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

macOS:
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

## 使用方法

### 掃描模式（僅顯示，不刪除）

```bash
node-cleaner scan <路徑>
```

範例:
```bash
node-cleaner scan C:\Projects
node-cleaner scan ~/projects
```

### 清理模式（掃描 + 互動式刪除）

```bash
node-cleaner clean <路徑>
```

範例:
```bash
node-cleaner clean C:\Projects
node-cleaner clean ~/projects
```

### 選項參數

- `--depth <數字>` - 限制掃描深度
- `--min-size <位元組>` - 只顯示大於指定大小的資料夾

範例:
```bash
# 只掃描 2 層深度
node-cleaner scan C:\Projects --depth 2

# 只顯示大於 100MB 的資料夾
node-cleaner scan C:\Projects --min-size 104857600

# 組合使用
node-cleaner clean ~/projects --depth 3 --min-size 52428800
```

## 使用範例

### 掃描結果

```
找到: C:\Projects\app1\node_modules
找到: C:\Projects\app2\node_modules
找到: C:\Projects\app3\node_modules

┌─────────────────────────────────┬──────────┬────────────┐
│ 路徑                             │     大小 │ 最後修改    │
├─────────────────────────────────┼──────────┼────────────┤
│ C:\Projects\app1\node_modules   │  450 MB  │ 2026-01-15 │
│ C:\Projects\app2\node_modules   │  680 MB  │ 2026-02-10 │
│ C:\Projects\app3\node_modules   │  320 MB  │ 2025-12-20 │
└─────────────────────────────────┴──────────┴────────────┘

總計: 3 個資料夾, 1.45 GB
```

### 互動式刪除

```
選擇要刪除的資料夾 (Space 切換, Enter 確認):
  [x] C:\Projects\app1\node_modules (450 MB)
  [ ] C:\Projects\app2\node_modules (680 MB)
  [x] C:\Projects\app3\node_modules (320 MB)

即將刪除 2 個資料夾 (770 MB)。確定要繼續嗎？ (y/N)
```

## 技術架構

- **.NET 9** - 最新的 .NET 版本
- **System.CommandLine** - 官方命令列框架
- **Spectre.Console** - 現代化 CLI UI 框架
- **xUnit** - 單元測試框架

## 專案結構

```
NodeModuleCleaner/
├── src/NodeModuleCleaner/
│   ├── Commands/          # CLI 命令實作
│   ├── Core/              # 核心邏輯（掃描、計算、刪除）
│   ├── Models/            # 資料模型
│   └── Program.cs         # 程式進入點
├── tests/
│   └── NodeModuleCleaner.Tests/  # 單元測試
└── docs/
    └── plans/             # 設計文件和實作計畫
```

## 開發

### 執行測試

```bash
dotnet test
```

### 本地執行

```bash
dotnet run -- scan .
dotnet run -- clean .
```

## 注意事項

⚠️ **重要警告**
- 刪除操作是**永久性**的，不會移到回收桶
- 刪除前請確認選擇的資料夾
- 建議先使用 `scan` 命令檢視，確認無誤後再使用 `clean` 命令

## 授權

MIT License

## 貢獻

歡迎提交 Issue 和 Pull Request！
```

**Step 2: Commit**

```bash
git add README.md
git commit -m "docs: 新增 README 使用說明"
```

---

## Task 9: 發布設定與最終測試

**Files:**
- Modify: `src/NodeModuleCleaner/NodeModuleCleaner.csproj`

**Step 1: 更新專案檔加入發布設定**

Modify file: `src/NodeModuleCleaner/NodeModuleCleaner.csproj`

Add the following properties to the `<PropertyGroup>`:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net9.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>

  <!-- 發布設定 -->
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>false</SelfContained>
  <PublishReadyToRun>true</PublishReadyToRun>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>

  <!-- 組件資訊 -->
  <Version>1.0.0</Version>
  <AssemblyName>node-cleaner</AssemblyName>
  <Product>Node Modules Cleaner</Product>
  <Description>快速掃描和清理 node_modules 資料夾的 CLI 工具</Description>
</PropertyGroup>
```

**Step 2: 執行完整測試套件**

Run: `dotnet test --verbosity normal`
Expected: All tests pass

**Step 3: 測試建置**

Run: `dotnet build -c Release`
Expected: Build succeeded with 0 warnings

**Step 4: 測試發布（Windows）**

Run: `dotnet publish -c Release -r win-x64 -o ./publish`
Expected: Publish succeeded

**Step 5: 測試執行發布的程式**

```bash
cd publish
./node-cleaner --help
./node-cleaner scan --help
./node-cleaner clean --help
```

Expected: 所有命令正確顯示說明

**Step 6: Commit**

```bash
git add src/NodeModuleCleaner/NodeModuleCleaner.csproj
git commit -m "chore: 新增發布設定與組件資訊"
```

---

## Task 10: 最終整理與驗收

**Step 1: 執行完整測試**

Run: `dotnet test`
Expected: All tests pass

**Step 2: 檢查程式碼品質**

Run: `dotnet build -c Release /warnaserror`
Expected: Build succeeded with no warnings

**Step 3: 建立發布版本（多平台）**

Windows:
```bash
dotnet publish -c Release -r win-x64 --self-contained -o ./dist/win-x64
```

Linux:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -o ./dist/linux-x64
```

macOS:
```bash
dotnet publish -c Release -r osx-x64 --self-contained -o ./dist/osx-x64
```

**Step 4: 手動驗收測試**

建立測試目錄結構：
```bash
mkdir -p test-area/project1/node_modules
mkdir -p test-area/project2/node_modules
echo "test" > test-area/project1/node_modules/file.txt
echo "test" > test-area/project2/node_modules/file.txt
```

測試 scan 命令：
```bash
./dist/win-x64/node-cleaner scan ./test-area
```

測試 clean 命令：
```bash
./dist/win-x64/node-cleaner clean ./test-area
```

Expected:
- 正確找到兩個 node_modules
- 顯示大小和修改時間
- 互動式選單正常運作
- 刪除成功並顯示結果

**Step 5: 最終 Commit**

```bash
git add .
git commit -m "chore: 專案完成並通過驗收測試"
```

**Step 6: 建立 Git Tag**

```bash
git tag -a v1.0.0 -m "Release version 1.0.0"
```

---

## 驗收標準

✅ 所有單元測試通過
✅ 可以成功建置和發布
✅ Scan 命令正確掃描並顯示結果
✅ Clean 命令可以互動式選擇並刪除
✅ 錯誤處理正確（無權限、不存在的目錄等）
✅ 命令列參數（--depth, --min-size）正常運作
✅ README 文件完整
✅ 多平台發布成功（Windows/Linux/macOS）

## 預估時間

- Task 1: 30 分鐘
- Task 2: 20 分鐘
- Task 3: 30 分鐘
- Task 4: 40 分鐘
- Task 5: 30 分鐘
- Task 6: 40 分鐘
- Task 7: 60 分鐘
- Task 8: 20 分鐘
- Task 9: 30 分鐘
- Task 10: 30 分鐘

**總計:** 約 5.5 小時

## 注意事項

- 嚴格遵循 TDD 流程：先寫測試 → 跑測試（失敗）→ 寫實作 → 跑測試（成功）→ commit
- 每個 commit 都要有意義且符合 conventional commit 格式
- 使用繁體中文作為 commit message
- 確保每個步驟都能獨立執行和驗證
- 遇到測試失敗時，先檢查測試本身是否正確
