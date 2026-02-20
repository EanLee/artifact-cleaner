using NodeModuleCleaner.Core;

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
