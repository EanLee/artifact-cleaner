namespace ArtifactCleaner.Models;

public class AppConfig
{
    public List<string> Targets { get; set; } =
    [
        "node_modules",     // npm / pnpm / yarn / bun
        ".pnpm-store",      // pnpm store
        "obj",              // .NET intermediate output
        "packages",         // 舊式 NuGet packages 資料夾
        ".nuget",           // 專案內 NuGet 還原快取
        "CMakeFiles",       // CMake 中繼檔
        "cmake-build-debug",
        "cmake-build-release",
    ];
}
