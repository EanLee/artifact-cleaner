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

        // .git / .vs 下的 node_modules 應被跳過
        Directory.CreateDirectory(Path.Combine(_testDir, ".git", "node_modules"));
        Directory.CreateDirectory(Path.Combine(_testDir, ".vs", "node_modules"));

        var validNodeModules = Path.Combine(_testDir, "project", "node_modules");
        Directory.CreateDirectory(validNodeModules);

        // Act
        var results = scanner.ScanDirectory(_testDir).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(validNodeModules, results[0].FullName);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ScanDirectory_CustomTargets_FindsBinAndObj()
    {
        // Arrange
        var scanner = new NodeModulesScanner();
        var bin = Path.Combine(_testDir, "MyProject", "bin");
        var obj = Path.Combine(_testDir, "MyProject", "obj");
        Directory.CreateDirectory(bin);
        Directory.CreateDirectory(obj);

        // Act
        var results = scanner.ScanDirectory(_testDir, targets: ["bin", "obj"]).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, d => d.FullName == bin);
        Assert.Contains(results, d => d.FullName == obj);

        // Cleanup
        Directory.Delete(_testDir, true);
    }

    [Fact]
    public void ScanDirectory_CustomTargets_DoesNotRecurseIntoTarget()
    {
        // Arrange
        var scanner = new NodeModulesScanner();
        var outerBin = Path.Combine(_testDir, "MyProject", "bin");
        var innerBin = Path.Combine(outerBin, "Debug", "bin"); // bin 內還有 bin，不應被找到
        Directory.CreateDirectory(innerBin);

        // Act
        var results = scanner.ScanDirectory(_testDir, targets: ["bin"]).ToList();

        // Assert - 只找到外層 bin
        Assert.Single(results);
        Assert.Equal(outerBin, results[0].FullName);

        // Cleanup
        Directory.Delete(_testDir, true);
    }
}
