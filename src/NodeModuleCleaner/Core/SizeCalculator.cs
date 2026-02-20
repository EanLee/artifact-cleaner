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
            // IgnoreInaccessible: 自動跳過無權限的目錄
            // AttributesToSkip ReparsePoint: 跳過 junction/symlink（pnpm .pnpm 目錄）
            //   避免 Windows 對「不受信任的掛接點」拋出 IOException
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };

            return directory
                .EnumerateFiles("*", options)
                .AsParallel()
                .Sum(file =>
                {
                    try
                    {
                        return file.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return 0;
                    }
                    catch (FileNotFoundException)
                    {
                        return 0;
                    }
                    catch (IOException)
                    {
                        return 0;
                    }
                });
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
        catch (DirectoryNotFoundException)
        {
            return 0;
        }
        catch (IOException)
        {
            // 遇到不受信任的掛接點（如 pnpm junction）時回傳 0
            return 0;
        }
    }
}
