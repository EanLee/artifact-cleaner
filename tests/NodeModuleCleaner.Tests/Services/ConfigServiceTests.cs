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
        Assert.True(File.Exists(_tempPath)); // verify file was actually written

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
