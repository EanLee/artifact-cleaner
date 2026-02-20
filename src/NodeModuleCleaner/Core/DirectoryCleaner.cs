namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責刪除資料夾
/// </summary>
public class DirectoryCleaner
{
    /// <summary>
    /// 刪除指定的資料夾及其所有內容
    /// </summary>
    /// <param name="directory">要刪除的資料夾</param>
    /// <param name="error">錯誤訊息（如果刪除失敗）</param>
    /// <returns>true 表示成功，false 表示失敗</returns>
    public bool DeleteDirectory(DirectoryInfo directory, out string? error)
    {
        error = null;

        try
        {
            if (!directory.Exists)
            {
                error = $"Directory not found: {directory.FullName}";
                return false;
            }

            DeleteRecursively(directory);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            error = $"Access denied: {ex.Message}";
            return false;
        }
        catch (IOException ex)
        {
            error = $"IO error: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Unexpected error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 自訂遞迴刪除：遇到 junction point / symlink 只刪連結本身，不跟進目標
    /// 避免 pnpm 的 .pnpm 目錄造成 Access Denied
    /// </summary>
    private static void DeleteRecursively(DirectoryInfo directory)
    {
        foreach (var subDir in directory.EnumerateDirectories())
        {
            if (subDir.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                // junction point / symlink：只刪連結，不動目標內容
                subDir.Delete(recursive: false);
            }
            else
            {
                DeleteRecursively(subDir);
            }
        }

        foreach (var file in directory.EnumerateFiles())
        {
            // 移除唯讀屬性，避免部分 npm 套件設為唯讀
            if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                file.Attributes = FileAttributes.Normal;
            }
            file.Delete();
        }

        directory.Delete(recursive: false);
    }
}
