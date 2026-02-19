# Node Modules Cleaner 設計文件

**日期：** 2026-02-20
**版本：** 1.0
**狀態：** 已批准

## 專案概述

Node Modules Cleaner 是一個 .NET 9 命令列工具，用於快速掃描、統計和清理指定目錄下所有的 `node_modules` 資料夾，幫助開發者釋放硬碟空間。

### 核心需求
- 遞迴掃描指定目錄找出所有 `node_modules`
- 統計每個資料夾的大小和總計
- 提供互動式選單讓使用者選擇要刪除的資料夾
- 簡潔清晰的表格式輸出

### 技術棧
- **.NET 9** - 最新的 .NET 版本
- **System.CommandLine** - 官方命令列框架
- **Spectre.Console** - 現代化 CLI UI 框架
- **xUnit** - 單元測試框架

---

## 整體架構

### 專案結構
```
NodeModuleCleaner/
├── NodeModuleCleaner.csproj    # .NET 9 主專案
├── Program.cs                   # 程式進入點，設定命令列
├── Commands/                    # 命令實作
│   ├── ScanCommand.cs          # scan 子命令
│   └── CleanCommand.cs         # clean 子命令（掃描+刪除）
├── Core/                        # 核心邏輯
│   ├── NodeModulesScanner.cs   # 掃描 node_modules
│   ├── SizeCalculator.cs       # 計算資料夾大小
│   └── NodeModuleCleaner.cs    # 刪除邏輯
└── Models/                      # 資料模型
    └── ScanResult.cs           # 掃描結果資料結構
```

### 使用方式

#### 掃描模式（僅顯示，不刪除）
```bash
node-cleaner scan C:\Projects
```

#### 清理模式（掃描 + 互動式刪除）
```bash
node-cleaner clean C:\Projects
```

#### 支援的選項
```bash
--depth <number>        # 限制掃描深度（預設無限）
--min-size <size>       # 只顯示大於指定大小的（例如 100MB）
```

---

## 核心組件設計

### 1. NodeModulesScanner

**職責：** 遞迴掃描指定目錄，找出所有 `node_modules` 資料夾

**介面：**
```csharp
public class NodeModulesScanner
{
    public IEnumerable<DirectoryInfo> ScanDirectory(string rootPath, int? maxDepth = null);
}
```

**實作細節：**
- 使用 `Directory.EnumerateDirectories` 進行遞迴搜尋
- 自動跳過常見的系統資料夾（`.git`, `.vs`, `bin`, `obj` 等）
- 支援深度限制，避免無限深入
- 找到 `node_modules` 時立即回傳（使用 `yield return`）

### 2. SizeCalculator

**職責：** 計算資料夾佔用的磁碟空間

**介面：**
```csharp
public class SizeCalculator
{
    public long CalculateSize(DirectoryInfo directory);
}
```

**實作細節：**
- 遞迴計算所有子檔案和子資料夾的大小
- 使用 `FileInfo.Length` 取得檔案大小
- 處理權限不足的錯誤（捕捉 `UnauthorizedAccessException`）
- 無法存取的檔案計為 0 bytes

### 3. NodeModuleCleaner

**職責：** 刪除選定的 `node_modules` 資料夾

**介面：**
```csharp
public class NodeModuleCleaner
{
    public bool DeleteDirectory(DirectoryInfo directory, out string error);
}
```

**實作細節：**
- 使用 `Directory.Delete(path, recursive: true)` 遞迴刪除
- 捕捉並回傳所有可能的錯誤（權限不足、檔案被鎖定等）
- 回傳布林值表示成功或失敗，錯誤訊息透過 `out` 參數回傳

### 4. ScanResult Model

**資料模型：**
```csharp
public record ScanResult(
    string Path,           // node_modules 完整路徑
    long SizeInBytes,      // 佔用空間（bytes）
    DateTime LastModified  // 最後修改時間
);
```

---

## 資料流與使用者互動

### Scan 命令流程

```
1. 使用者執行：node-cleaner scan C:\Projects
   ↓
2. NodeModulesScanner 開始掃描
   ├─ 顯示 Spinner：「Scanning for node_modules...」
   └─ 每找到一個資料夾即時顯示路徑
   ↓
3. 對每個找到的資料夾調用 SizeCalculator
   ├─ 計算大小
   └─ 建立 ScanResult 物件
   ↓
4. 收集所有結果
   ↓
5. 使用 Spectre.Console Table 顯示結果：
   ┌────────────────────────────────┬──────────┬─────────────┐
   │ Path                           │ Size     │ Modified    │
   ├────────────────────────────────┼──────────┼─────────────┤
   │ C:\Projects\app1\node_modules  │ 450 MB   │ 2026-01-15  │
   │ C:\Projects\app2\node_modules  │ 680 MB   │ 2026-02-10  │
   └────────────────────────────────┴──────────┴─────────────┘

   Total: 2 folders, 1.13 GB
```

### Clean 命令流程

```
1. 使用者執行：node-cleaner clean C:\Projects
   ↓
2. 執行掃描流程（同上 1-4）
   ↓
3. 顯示結果表格
   ↓
4. 使用 Spectre.Console MultiSelectionPrompt：
   ┌─ Select folders to delete ────────────────────────────┐
   │ Use Space to toggle, Enter to confirm                 │
   │                                                        │
   │ [x] C:\Projects\app1\node_modules (450 MB)            │
   │ [ ] C:\Projects\app2\node_modules (680 MB)            │
   │ [x] C:\Projects\app3\node_modules (320 MB)            │
   └────────────────────────────────────────────────────────┘
   ↓
5. 使用者選擇完畢後，顯示確認提示：
   「About to delete 2 folders (770 MB). Continue? (y/n)」
   ↓
6. 確認後逐一刪除：
   ├─ 顯示進度條
   ├─ 成功：✓ Deleted C:\Projects\app1\node_modules
   └─ 失敗：✗ Failed to delete C:\Projects\app3\node_modules
           (錯誤訊息)
   ↓
7. 顯示刪除結果摘要：
   Successfully deleted: 1 folder (450 MB)
   Failed: 1 folder
   Total freed: 450 MB
```

---

## 錯誤處理策略

### 掃描階段錯誤

**1. 根目錄不存在或無權限存取**
- **處理方式：** 立即顯示錯誤訊息並結束程式（exit code 1）
- **訊息範例：** `❌ Error: Cannot access 'C:\Projects' - Directory not found`

**2. 掃描過程中遇到無權限的子資料夾**
- **處理方式：** 跳過該資料夾，繼續掃描其他位置
- **訊息範例：** 在最後顯示 `⚠ Warning: Skipped 3 folders due to permission errors`

**3. 計算大小時發生錯誤**
- **處理方式：** 該資料夾大小顯示為 "Unknown"
- **注意：** 仍然可以選擇刪除，但會在選單中標記 (Size unknown)

### 刪除階段錯誤

**1. 資料夾被鎖定或檔案使用中**
- **處理方式：** 記錄錯誤，繼續刪除其他資料夾
- **訊息範例：** `❌ Failed: C:\...\node_modules - File is in use by another process`

**2. 權限不足**
- **處理方式：** 記錄錯誤，繼續處理
- **訊息範例：** `❌ Failed: C:\...\node_modules - Access denied`

**3. 刪除過程中使用者中斷（Ctrl+C）**
- **處理方式：**
  - 捕捉 `ConsoleCancelEventHandler`
  - 顯示已刪除和未刪除的清單
  - 提示使用者已刪除的資料夾無法恢復

### 錯誤處理原則

- **友善訊息：** 所有錯誤都轉換為使用者友善的訊息，不顯示 stack trace
- **關鍵錯誤終止：** 無法繼續的錯誤（如根目錄不存在）會終止程式
- **非關鍵錯誤繼續：** 部分資料夾失敗不影響其他操作
- **明確的成功/失敗狀態：** 使用顏色和符號（✓/✗）清楚標示結果

---

## 測試策略

### 單元測試（xUnit）

**SizeCalculator 測試**
```csharp
[Fact]
public void CalculateSize_EmptyDirectory_ReturnsZero()

[Fact]
public void CalculateSize_DirectoryWithFiles_ReturnsCorrectSize()

[Fact]
public void CalculateSize_NestedDirectories_CalculatesRecursively()
```

**NodeModulesScanner 測試**
```csharp
[Fact]
public void ScanDirectory_FindsNodeModules_ReturnsCorrectPaths()

[Fact]
public void ScanDirectory_WithDepthLimit_RespectsMaxDepth()

[Fact]
public void ScanDirectory_SkipsSystemFolders()
```

### 不進行單元測試的部分
- **Spectre.Console UI：** 互動式元件，手動測試即可
- **實際刪除操作：** 太危險且難以自動化，僅手動測試
- **Command 層：** 程式碼太薄（主要是串接），整合測試覆蓋即可

### 手動測試計畫

測試環境：建立包含多個測試專案的目錄結構

**測試情境：**
1. **正常掃描：** 包含多個 node_modules 的目錄
2. **選擇性刪除：** 互動式選單選擇部分刪除
3. **權限錯誤：** 測試無權限存取的資料夾
4. **檔案鎖定：** 測試刪除使用中的 node_modules
5. **命令列參數：** 測試 `--depth` 和 `--min-size` 選項
6. **邊界條件：** 空目錄、沒有 node_modules、超深層級

### 測試覆蓋率目標
- **核心邏輯（Scanner, Calculator）：** 80% 以上
- **整體專案：** 不強求高覆蓋率（因為包含大量 UI 互動）

---

## 非功能性需求

### 效能
- **掃描速度：** 使用非同步 I/O 和平行處理（待實作階段評估）
- **記憶體：** 使用 `yield return` 避免一次載入所有結果
- **回應性：** 即時顯示找到的資料夾，不等掃描完成

### 安全性
- **二次確認：** 刪除前必須確認
- **無備份：** 明確告知使用者刪除是永久性的（不移到回收桶）
- **權限檢查：** 遇到無權限操作時適當處理，不會導致程式崩潰

### 可用性
- **清晰的輸出：** 使用表格和顏色提升可讀性
- **進度指示：** 長時間操作顯示 spinner 或進度條
- **錯誤訊息：** 所有錯誤都有明確的說明和建議

### 可維護性
- **模組化：** 清楚分離掃描、計算、刪除邏輯
- **可擴展：** 未來可輕易加入新功能（如匯出報告、過濾條件）
- **程式碼品質：** 遵循 C# 編碼規範，使用 record 和現代語法

---

## 未來可能的擴展（本次不實作）

- **匯出功能：** 將掃描結果匯出為 CSV 或 JSON
- **回收桶模式：** 刪除前先移到暫存位置
- **過濾條件：** `--older-than 30d` 或 `--larger-than 500MB`
- **平行刪除：** 同時刪除多個資料夾提升速度
- **配置檔：** 儲存常用的掃描路徑和選項
- **GUI 版本：** Avalonia 或 MAUI 圖形介面

---

## 交付物

1. **可執行檔：** 單一執行檔，支援 Windows/Linux/macOS
2. **原始碼：** 完整的 .NET 9 專案
3. **單元測試：** 核心邏輯的測試覆蓋
4. **README：** 使用說明和範例

## 時程預估

- **專案建立與套件安裝：** 30 分鐘
- **核心邏輯開發：** 2-3 小時
- **命令與 UI 整合：** 1-2 小時
- **測試與除錯：** 1 小時
- **文件撰寫：** 30 分鐘

**總計：** 約 5-7 小時開發時間

---

## 結論

Node Modules Cleaner 是一個聚焦、實用的工具，使用現代化的 .NET 9 技術棧和 Spectre.Console 提供良好的使用者體驗。設計遵循 YAGNI 原則，只包含必要的核心功能，同時保持良好的可擴展性以應對未來需求。
