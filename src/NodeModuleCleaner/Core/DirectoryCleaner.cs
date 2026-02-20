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

            // 遞迴刪除資料夾及所有內容
            Directory.Delete(directory.FullName, recursive: true);
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
}
