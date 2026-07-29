# Artifact Cleaner

一個快速、簡單的 .NET CLI 工具，用於掃描、統計和清理專案目錄下的建置產物資料夾（預設為 `node_modules`，可自訂為 `bin`、`obj` 等其他目標）。

## 功能特色

- 🔍 **快速掃描** - 遞迴搜尋指定目錄下的所有目標資料夾
- 📊 **詳細統計** - 顯示每個資料夾的大小和最後修改時間
- 🎯 **互動式選擇** - 使用方向鍵和空白鍵選擇要刪除的資料夾
- 🎨 **美觀的介面** - 使用 Spectre.Console 提供現代化 CLI 體驗
- ⚡ **效能優化** - 使用 yield return 和非同步 I/O 提升效能
- 🛠️ **可自訂目標** - 透過 `config` 指令或 `--folder` 選項調整要清理的資料夾名稱

## 安裝

### 從原始碼建置

```bash
git clone <repository-url>
cd artifact-cleaner
dotnet build -c Release
```

### 發布為單一執行檔

Windows:
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Linux:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

macOS:
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

## 使用方法

### 掃描模式（僅顯示，不刪除）

```bash
artifact-cleaner scan <路徑>
```

範例:
```bash
artifact-cleaner scan C:\Projects
artifact-cleaner scan ~/projects
```

### 清理模式（掃描 + 互動式刪除）

```bash
artifact-cleaner clean <路徑>
```

範例:
```bash
artifact-cleaner clean C:\Projects
artifact-cleaner clean ~/projects
```

### 選項參數

- `--depth <數字>` - 限制掃描深度
- `--min-size <位元組>` - 只顯示大於指定大小的資料夾
- `--older-than <天數>` - 只顯示最後修改時間超過指定天數的資料夾
- `--folder <名稱...>` - 臨時覆蓋要掃描的目標資料夾名稱（不影響 config，可指定多個）

範例:
```bash
# 只掃描 2 層深度
artifact-cleaner scan C:\Projects --depth 2

# 只顯示大於 100MB 的資料夾
artifact-cleaner scan C:\Projects --min-size 104857600

# 只顯示 90 天前就沒更新過的資料夾
artifact-cleaner scan C:\Projects --older-than 90

# 臨時掃描 bin/obj 而非預設的 node_modules
artifact-cleaner scan C:\Projects --folder bin obj

# 組合使用
artifact-cleaner clean ~/projects --depth 3 --min-size 52428800 --older-than 30
```

### 設定目標資料夾

預設只掃描 `node_modules`。若要長期改變目標（例如改成 `bin`、`obj`），可透過 `config` 指令調整：

```bash
artifact-cleaner config list             # 顯示目前設定的目標資料夾
artifact-cleaner config add bin obj      # 新增目標資料夾
artifact-cleaner config remove obj       # 移除目標資料夾
artifact-cleaner config reset            # 重設為預設值 (node_modules)
```

## 使用範例

### 掃描結果

```
找到: C:\Projects\app1\node_modules
找到: C:\Projects\app2\node_modules
找到: C:\Projects\app3\node_modules

┌─────────────────────────────────┬──────────┬────────────┐
│ 路徑                             │     大小 │ 最後修改    │
├─────────────────────────────────┼──────────┼────────────┤
│ C:\Projects\app1\node_modules   │  450 MB  │ 2026-01-15 │
│ C:\Projects\app2\node_modules   │  680 MB  │ 2026-02-10 │
│ C:\Projects\app3\node_modules   │  320 MB  │ 2025-12-20 │
└─────────────────────────────────┴──────────┴────────────┘

總計: 3 個資料夾, 1.45 GB
```

### 互動式刪除

```
選擇要刪除的資料夾 (Space 切換, Enter 確認):
  [x] C:\Projects\app1\node_modules (450 MB)
  [ ] C:\Projects\app2\node_modules (680 MB)
  [x] C:\Projects\app3\node_modules (320 MB)

即將刪除 2 個資料夾 (770 MB)。確定要繼續嗎？ (y/N)
```

## 技術架構

- **.NET 9** - 最新的 .NET 版本
- **System.CommandLine** - 官方命令列框架
- **Spectre.Console** - 現代化 CLI UI 框架
- **xUnit** - 單元測試框架

## 專案結構

```
ArtifactCleaner/
├── src/ArtifactCleaner/
│   ├── Commands/          # CLI 命令實作
│   ├── Core/              # 核心邏輯（掃描、計算、刪除）
│   ├── Models/            # 資料模型
│   └── Program.cs         # 程式進入點
├── tests/
│   └── ArtifactCleaner.Tests/  # 單元測試
└── docs/
    └── plans/             # 設計文件和實作計畫
```

## 開發

### 執行測試

```bash
dotnet test
```

### 本地執行

```bash
dotnet run -- scan .
dotnet run -- clean .
```

## 注意事項

⚠️ **重要警告**
- 刪除操作是**永久性**的，不會移到回收桶
- 刪除前請確認選擇的資料夾
- 建議先使用 `scan` 命令檢視，確認無誤後再使用 `clean` 命令

## 授權

MIT License

## 貢獻

歡迎提交 Issue 和 Pull Request！
