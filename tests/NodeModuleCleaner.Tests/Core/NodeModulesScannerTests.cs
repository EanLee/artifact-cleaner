using NodeModuleCleaner.Core;

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
