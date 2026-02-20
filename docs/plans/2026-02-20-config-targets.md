# Config Targets Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 讓使用者透過 `config` 命令持久化目標資料夾名稱，`scan`/`clean` 自動讀取，不需每次帶參數。

**Architecture:** 新增 `ConfigService` 讀寫執行檔同目錄的 `node-cleaner.json`；`NodeModulesScanner` 改為接受可設定的目標名稱清單；`BaseCommand` 自動載入 config 傳入 scanner。

**Tech Stack:** .NET 9, System.Text.Json（內建）, Spectre.Console, xUnit

---

### Task 1: AppConfig model + ConfigService

**Files:**
- Create: `src/NodeModuleCleaner/Models/AppConfig.cs`
- Create: `src/NodeModuleCleaner/Services/ConfigService.cs`
- Create: `tests/NodeModuleCleaner.Tests/Services/ConfigServiceTests.cs`

**Step 1: 建立 AppConfig model**

```csharp
// src/NodeModuleCleaner/Models/AppConfig.cs
namespace NodeModuleCleaner.Models;

public class AppConfig
{
    public List<string> Targets { get; set; } = ["node_modules"];
}
```

**Step 2: 建立 ConfigService**

```csharp
// src/NodeModuleCleaner/Services/ConfigService.cs
using NodeModuleCleaner.Models;
using System.Text.Json;

namespace NodeModuleCleaner.Services;

public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath;

    public ConfigService(string? configPath = null)
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        _configPath = configPath ?? Path.Combine(exeDir, "node-cleaner.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    public string ConfigPath => _configPath;
}
```

**Step 3: 寫測試**

```csharp
// tests/NodeModuleCleaner.Tests/Services/ConfigServiceTests.cs
using NodeModuleCleaner.Services;

namespace NodeModuleCleaner.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly ConfigService _service;

    public ConfigServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"config_test_{Guid.NewGuid()}.json");
        _service = new ConfigService(_tempPath);
    }

    [Fact]
    public void Load_NoFile_ReturnsDefault()
    {
        var config = _service.Load();
        Assert.Equal(["node_modules"], config.Targets);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var config = _service.Load();
        config.Targets = ["bin", "obj"];
        _service.Save(config);

        var loaded = _service.Load();
        Assert.Equal(["bin", "obj"], loaded.Targets);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefault()
    {
        File.WriteAllText(_tempPath, "not valid json {{");
        var config = _service.Load();
        Assert.Equal(["node_modules"], config.Targets);
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))
            File.Delete(_tempPath);
    }
}
```

**Step 4: 執行測試確認通過**

```
dotnet test tests/NodeModuleCleaner.Tests --filter "ConfigServiceTests" -v
```
Expected: 3 passed

**Step 5: Commit**

```bash
git add src/NodeModuleCleaner/Models/AppConfig.cs \
        src/NodeModuleCleaner/Services/ConfigService.cs \
        tests/NodeModuleCleaner.Tests/Services/ConfigServiceTests.cs
git commit -m "feat: 新增 AppConfig 與 ConfigService"
```

---

### Task 2: ConfigCommand

**Files:**
- Create: `src/NodeModuleCleaner/Commands/ConfigCommand.cs`
- Modify: `src/NodeModuleCleaner/Program.cs`

**Step 1: 建立 ConfigCommand**

```csharp
// src/NodeModuleCleaner/Commands/ConfigCommand.cs
using NodeModuleCleaner.Services;
using Spectre.Console;
using System.CommandLine;

namespace NodeModuleCleaner.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "管理目標資料夾設定");
        command.Subcommands.Add(CreateListCommand());
        command.Subcommands.Add(CreateAddCommand());
        command.Subcommands.Add(CreateRemoveCommand());
        command.Subcommands.Add(CreateResetCommand());
        return command;
    }

    private static Command CreateListCommand()
    {
        var cmd = new Command("list", "顯示目前設定的目標資料夾");
        cmd.SetAction(_ =>
        {
            var service = new ConfigService();
            var config = service.Load();
            AnsiConsole.MarkupLine($"[dim]Config: {service.ConfigPath}[/]");
            AnsiConsole.MarkupLine("[bold]目標資料夾：[/]");
            foreach (var t in config.Targets)
                AnsiConsole.MarkupLine($"  [cyan]{t}[/]");
        });
        return cmd;
    }

    private static Command CreateAddCommand()
    {
        var cmd = new Command("add", "新增目標資料夾");
        var namesArg = new Argument<string[]>("names") { Description = "資料夾名稱（可多個）" };
        namesArg.Arity = ArgumentArity.OneOrMore;
        cmd.Arguments.Add(namesArg);
        cmd.SetAction(parseResult =>
        {
            var names = parseResult.GetValue(namesArg)!;
            var service = new ConfigService();
            var config = service.Load();
            var added = new List<string>();
            foreach (var name in names)
            {
                if (!config.Targets.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    config.Targets.Add(name);
                    added.Add(name);
                }
            }
            service.Save(config);
            if (added.Count > 0)
                AnsiConsole.MarkupLine($"[green]✓[/] 已新增: {string.Join(", ", added)}");
            else
                AnsiConsole.MarkupLine("[yellow]已存在，無需新增[/]");
        });
        return cmd;
    }

    private static Command CreateRemoveCommand()
    {
        var cmd = new Command("remove", "移除目標資料夾");
        var namesArg = new Argument<string[]>("names") { Description = "資料夾名稱（可多個）" };
        namesArg.Arity = ArgumentArity.OneOrMore;
        cmd.Arguments.Add(namesArg);
        cmd.SetAction(parseResult =>
        {
            var names = parseResult.GetValue(namesArg)!;
            var service = new ConfigService();
            var config = service.Load();
            var removed = config.Targets.RemoveAll(t =>
                names.Any(n => string.Equals(n, t, StringComparison.OrdinalIgnoreCase)));
            service.Save(config);
            AnsiConsole.MarkupLine($"[green]✓[/] 已移除 {removed} 個");
        });
        return cmd;
    }

    private static Command CreateResetCommand()
    {
        var cmd = new Command("reset", "重設為預設值 (node_modules)");
        cmd.SetAction(_ =>
        {
            var service = new ConfigService();
            service.Save(new NodeModuleCleaner.Models.AppConfig());
            AnsiConsole.MarkupLine("[green]✓[/] 已重設為預設值");
        });
        return cmd;
    }
}
```

**Step 2: 在 Program.cs 註冊 ConfigCommand**

```csharp
// src/NodeModuleCleaner/Program.cs 加入這行（在 CleanCommand 之後）：
rootCommand.Subcommands.Add(ConfigCommand.Create());
```

**Step 3: Build 確認**

```
dotnet build src/NodeModuleCleaner -c Release
```
Expected: 0 errors

**Step 4: 手動測試 config 命令**

```
node-cleaner config list
node-cleaner config add bin obj
node-cleaner config list
node-cleaner config remove obj
node-cleaner config reset
```

**Step 5: Commit**

```bash
git add src/NodeModuleCleaner/Commands/ConfigCommand.cs \
        src/NodeModuleCleaner/Program.cs
git commit -m "feat: 新增 config 命令管理目標資料夾"
```

---

### Task 3: 更新 NodeModulesScanner 支援自訂目標

**Files:**
- Modify: `src/NodeModuleCleaner/Core/NodeModulesScanner.cs`
- Modify: `tests/NodeModuleCleaner.Tests/Core/NodeModulesScannerTests.cs`

**Step 1: 重構 NodeModulesScanner**

`SystemFolders` 移除 `bin`/`obj`（它們不是真正的系統資料夾，不應無條件跳過）。
`ScanDirectory` 加入 `targets` 參數，預設 `["node_modules"]`。

```csharp
// src/NodeModuleCleaner/Core/NodeModulesScanner.cs
namespace NodeModuleCleaner.Core;

public class NodeModulesScanner
{
    // 真正需要無條件跳過的隱藏/系統資料夾
    private static readonly HashSet<string> SystemFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", ".vscode", ".github"
    };

    public IEnumerable<DirectoryInfo> ScanDirectory(
        string rootPath,
        int? maxDepth = null,
        IEnumerable<string>? targets = null)
    {
        var rootDir = new DirectoryInfo(rootPath);
        if (!rootDir.Exists)
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");

        var targetSet = targets != null
            ? new HashSet<string>(targets, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(["node_modules"], StringComparer.OrdinalIgnoreCase);

        return ScanDirectoryInternal(rootDir, currentDepth: 0, maxDepth, targetSet);
    }

    private IEnumerable<DirectoryInfo> ScanDirectoryInternal(
        DirectoryInfo directory,
        int currentDepth,
        int? maxDepth,
        HashSet<string> targets)
    {
        if (maxDepth.HasValue && currentDepth > maxDepth.Value)
            yield break;

        IEnumerable<DirectoryInfo> subDirs;
        try
        {
            subDirs = directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var subDir in subDirs)
        {
            if (SystemFolders.Contains(subDir.Name))
                continue;

            if (targets.Contains(subDir.Name))
            {
                int targetDepth = currentDepth + 1;
                if (!maxDepth.HasValue || targetDepth <= maxDepth.Value)
                    yield return subDir;
                continue; // 不深入目標資料夾內部
            }

            foreach (var found in ScanDirectoryInternal(subDir, currentDepth + 1, maxDepth, targets))
                yield return found;
        }
    }
}
```

**Step 2: 更新測試**

`ScanDirectory_SkipsSystemFolders` 原本測試 `bin/node_modules` 被跳過，但 `bin` 已不在 SystemFolders，需更新測試：

```csharp
[Fact]
public void ScanDirectory_SkipsSystemFolders()
{
    var scanner = new NodeModulesScanner();

    // 真正的系統資料夾：.git, .vs 下的 node_modules 應被跳過
    Directory.CreateDirectory(Path.Combine(_testDir, ".git", "node_modules"));
    Directory.CreateDirectory(Path.Combine(_testDir, ".vs", "node_modules"));

    // 正常的 node_modules
    var validNodeModules = Path.Combine(_testDir, "project", "node_modules");
    Directory.CreateDirectory(validNodeModules);

    var results = scanner.ScanDirectory(_testDir).ToList();

    Assert.Single(results);
    Assert.Equal(validNodeModules, results[0].FullName);

    Directory.Delete(_testDir, true);
}

[Fact]
public void ScanDirectory_CustomTargets_FindsBinAndObj()
{
    var scanner = new NodeModulesScanner();
    var bin = Path.Combine(_testDir, "MyProject", "bin");
    var obj = Path.Combine(_testDir, "MyProject", "obj");
    Directory.CreateDirectory(bin);
    Directory.CreateDirectory(obj);

    var results = scanner.ScanDirectory(_testDir, targets: ["bin", "obj"]).ToList();

    Assert.Equal(2, results.Count);
    Assert.Contains(results, d => d.FullName == bin);
    Assert.Contains(results, d => d.FullName == obj);

    Directory.Delete(_testDir, true);
}

[Fact]
public void ScanDirectory_CustomTargets_DoesNotRecurseIntoTarget()
{
    var scanner = new NodeModulesScanner();
    // bin 下面還有 bin（不應該被找到）
    var outerBin = Path.Combine(_testDir, "MyProject", "bin");
    var innerBin = Path.Combine(outerBin, "Debug", "bin");
    Directory.CreateDirectory(innerBin);

    var results = scanner.ScanDirectory(_testDir, targets: ["bin"]).ToList();

    Assert.Single(results);
    Assert.Equal(outerBin, results[0].FullName);

    Directory.Delete(_testDir, true);
}
```

**Step 3: 執行所有測試**

```
dotnet test tests/NodeModuleCleaner.Tests -v
```
Expected: all passed

**Step 4: Commit**

```bash
git add src/NodeModuleCleaner/Core/NodeModulesScanner.cs \
        tests/NodeModuleCleaner.Tests/Core/NodeModulesScannerTests.cs
git commit -m "feat: NodeModulesScanner 支援自訂目標資料夾名稱"
```

---

### Task 4: BaseCommand 整合 Config + --folder override

**Files:**
- Modify: `src/NodeModuleCleaner/Commands/BaseCommand.cs`
- Modify: `src/NodeModuleCleaner/Commands/ScanCommand.cs`
- Modify: `src/NodeModuleCleaner/Commands/CleanCommand.cs`

**Step 1: BaseCommand 加入 --folder option 與 config 載入**

```csharp
// 在 BaseCommand 新增：
public static Option<string[]> CreateFolderOption()
{
    var opt = new Option<string[]>("--folder")
    {
        Description = "臨時覆蓋目標資料夾（不影響 config）",
        Arity = ArgumentArity.OneOrMore,
    };
    return opt;
}

// ScanNodeModulesAsync 加入 targets 參數：
public static async Task<List<ScanResult>> ScanNodeModulesAsync(
    string rootPath,
    int? maxDepth,
    long? minSize,
    IEnumerable<string>? targets = null)
{
    var scanner = new NodeModulesScanner();
    var calculator = new SizeCalculator();

    // 若未指定 targets，從 config 讀取
    var effectiveTargets = targets?.ToList()
        ?? new ConfigService().Load().Targets;

    var targetLabel = string.Join(", ", effectiveTargets);

    return await AnsiConsole.Status()
        .StartAsync($"掃描 [{targetLabel}] 資料夾中...", ctx =>
        {
            return Task.Run(() =>
            {
                try
                {
                    var directories = scanner.ScanDirectory(rootPath, maxDepth, effectiveTargets).ToList();
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
```

**Step 2: ScanCommand 加入 --folder**

```csharp
// 在 Create() 加入：
var folderOption = BaseCommand.CreateFolderOption();
command.Options.Add(folderOption);

// 在 ExecuteAsync 呼叫處：
var folders = parseResult.GetValue(folderOption);
await ExecuteAsync(path!, depth, minSize, folders);

// ExecuteAsync 簽名：
private static async Task ExecuteAsync(string rootPath, int? maxDepth, long? minSize, string[]? folders)
{
    var results = await BaseCommand.ScanNodeModulesAsync(rootPath, maxDepth, minSize, folders);
    // ... 其餘不變
}
```

**Step 3: CleanCommand 同樣加入 --folder**（同 ScanCommand 模式）

**Step 4: Build 確認**

```
dotnet build src/NodeModuleCleaner -c Release
```
Expected: 0 errors

**Step 5: Publish 並驗證端對端**

```
dotnet publish src/NodeModuleCleaner -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

# 測試 config 流程
./publish/node-cleaner.exe config list
./publish/node-cleaner.exe config add bin obj
./publish/node-cleaner.exe config list
./publish/node-cleaner.exe scan C:\Repos

# 測試臨時 override
./publish/node-cleaner.exe scan C:\Repos --folder node_modules
```

**Step 6: 執行所有測試**

```
dotnet test tests/NodeModuleCleaner.Tests -v
```

**Step 7: Commit**

```bash
git add src/NodeModuleCleaner/Commands/BaseCommand.cs \
        src/NodeModuleCleaner/Commands/ScanCommand.cs \
        src/NodeModuleCleaner/Commands/CleanCommand.cs
git commit -m "feat: scan/clean 自動讀取 config，支援 --folder 臨時覆蓋"
```
