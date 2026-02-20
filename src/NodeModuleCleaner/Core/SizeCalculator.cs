namespace NodeModuleCleaner.Core;

/// <summary>
/// 負責計算資料夾大小（優化版：使用 AllDirectories 避免遞迴）
/// </summary>
public class SizeCalculator
{
    /// <summary>
    /// 計算指定資料夾的總大小（包含所有子檔案和子資料夾）
    /// 優化：使用 SearchOption.AllDirectories 一次性取得所有檔案，避免遞迴
    /// </summary>
    /// <param name="directory">要計算的資料夾</param>
    /// <returns>總大小（bytes）</returns>
    public long CalculateSize(DirectoryInfo directory)
    {
        try
        {
            // 使用 AllDirectories 一次性取得所有檔案，避免遞迴
            return directory
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .AsParallel()  // 平行處理提升效能
                .Sum(file =>
                {
                    try
                    {
                        return file.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 跳過無法存取的檔案
                        return 0;
                    }
                    catch (FileNotFoundException)
                    {
                        // 跳過已被刪除的檔案
                        return 0;
                    }
                });
        }
        catch (UnauthorizedAccessException)
        {
            // 無法列舉資料夾內容
            return 0;
        }
        catch (DirectoryNotFoundException)
        {
            // 資料夾不存在
            return 0;
        }
    }
}
