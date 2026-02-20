namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責掃描指定目錄下的所有 node_modules 資料夾
/// </summary>
public class NodeModulesScanner
{
    // 真正需要無條件跳過的隱藏/系統資料夾（不含 bin/obj，它們可能是掃描目標）
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
                if (!maxDepth.HasValue || currentDepth + 1 <= maxDepth.Value)
                    yield return subDir;
                continue; // 不深入目標資料夾內部
            }

            foreach (var found in ScanDirectoryInternal(subDir, currentDepth + 1, maxDepth, targets))
                yield return found;
        }
    }
}
