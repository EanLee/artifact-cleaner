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
                // 檢查 node_modules 的深度是否超過限制
                int nodeModulesDepth = currentDepth + 1;
                if (!maxDepth.HasValue || nodeModulesDepth <= maxDepth.Value)
                {
                    yield return subDir;
                }
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
