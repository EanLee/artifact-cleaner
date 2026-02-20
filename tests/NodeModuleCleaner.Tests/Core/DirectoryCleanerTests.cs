using NodeModuleCleaner.Core;

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
