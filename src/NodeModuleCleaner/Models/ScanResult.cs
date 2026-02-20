namespace NodeModuleCleaner.Models;

/// <summary>
/// 代表一個 node_modules 資料夾的掃描結果
/// </summary>
public record ScanResult(
    string Path,
    long SizeInBytes,
    DateTime LastModified
);
