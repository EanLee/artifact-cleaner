namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責計算資料夾大小
/// </summary>
public class SizeCalculator
{
    /// <summary>
    /// 計算指定資料夾的總大小（包含所有子檔案和子資料夾）
    /// </summary>
    /// <param name="directory">要計算的資料夾</param>
    /// <returns>總大小（bytes）</returns>
    public long CalculateSize(DirectoryInfo directory)
    {
        long totalSize = 0;

        try
        {
            // 計算所有檔案大小
            foreach (var file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    totalSize += file.Length;
                }
                catch (UnauthorizedAccessException)
                {
                    // 跳過無法存取的檔案
                }
                catch (FileNotFoundException)
                {
                    // 跳過已被刪除的檔案
                }
            }

            // 遞迴計算子資料夾
            foreach (var subDir in directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    totalSize += CalculateSize(subDir);
                }
                catch (UnauthorizedAccessException)
                {
                    // 跳過無法存取的資料夾
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 無法列舉資料夾內容，回傳目前累計的大小
        }

        return totalSize;
    }
}
