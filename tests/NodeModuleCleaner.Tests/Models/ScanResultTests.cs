using NodeModuleCleaner.Models;

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
