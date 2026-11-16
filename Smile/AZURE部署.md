# 部署到 Azure（約 20 分鐘）

老師要求全部上雲。以下用 Visual Studio 的「發佈」完成，不需要另外裝 Azure CLI。
學生可以先申請 **Azure for Students**（用學校信箱，免信用卡，有 100 美元額度）。

## 1. 建立 Azure SQL 資料庫（免費方案）
1. 到 Azure 入口網站 → 建立資源 → **SQL Database**。
2. 畫面上若有「**套用免費供應項目**」(Free offer) 請點選，每月有免費額度；沒有的話選最便宜的「基本」層級。
3. 建立新的 SQL Server：驗證方式選「SQL 驗證」，自訂管理員帳號和密碼（請自己保管，不要寫進程式碼或簡報）。
4. 網路：勾選「**允許 Azure 服務和資源存取此伺服器**」。
5. 資料庫名稱填 `SmileDb`。建立好後是空的資料庫，**不用自己建資料表**，網站第一次開啟時會自動建表並放入示範資料（約 30 秒）。

## 2. 建立 App Service（網站）
1. Visual Studio → 在專案按右鍵 → **發佈** → Azure → **Azure App Service (Windows)** → 登入你的 Azure 帳號。
2. 新建 App Service：方案選 **F1（免費）** 或 B1；執行階段是 .NET Framework 4.8（Windows）。
3. 先不要按發佈，接著做第 3 步。

## 3. 在 App Service 設定連線字串與示範密碼
Azure 入口網站 → 你的 App Service → **設定（環境變數）**：

**連線字串**（名稱一定要是 `SmileContext`，類型選 SQLAzure）：
```
Server=tcp:<你的伺服器>.database.windows.net,1433;Initial Catalog=SmileDb;User ID=<管理員帳號>;Password=<密碼>;Encrypt=True;MultipleActiveResultSets=True;Connection Timeout=60;
```

**應用程式設定**（公開網站一定要改，不然任何人都能用 README 上的密碼登入管理者）：
| 名稱 | 值 |
|---|---|
| `DemoPassword:Admin` | 自訂的強密碼 |
| `DemoPassword:Merchant` | 自訂的強密碼 |
| `DemoPassword:Member` | 自訂的強密碼 |
| `DemoPassword:Friend` | 自訂的強密碼 |

> 這些設定只在**第一次建立資料庫**時使用。如果已經用預設密碼建好了，請登入後到「帳號設定」改密碼。

## 4. 發佈
回到 Visual Studio 按 **發佈**。完成後打開 `https://<你的網站>.azurewebsites.net`，第一次會比較慢（建資料表＋匯入 1,428 間店）。

## 常見問題
- **出現 500 錯誤**：多半是連線字串打錯，或 SQL Server 防火牆沒有允許 Azure 服務。
- **相機掃碼打不開**：要用 `https://` 網址開啟。
- **店家上傳的照片**：存在網站的 `Uploads` 資料夾。重新發佈時若勾選「刪除目的地的其他檔案」會被刪掉，請不要勾。
- **費用**：F1 方案與 SQL 免費供應項目不收費；用完記得到入口網站刪除資源群組，避免之後被收費。
