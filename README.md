# 學區生活圈

臺中科大、臺中一中、臺體大周邊兩公里的店家與優惠平台：找店、領券、到店出示 QR Code、集點、送券給好友，店家零抽成。

「動態網頁程式設計」課程期末專題（小組作品）。

## 功能
- **消費者**：首頁輪播與活動、地圖／列表找店（OpenStreetMap 1,428 間真實店家）、連續逛店（上一家／下一家、左右滑）、今天吃什麼、領券與 QR Code、傳給家人、好友與送券、會員卡集點、手機條碼載具、每日簽到抽抽樂、等級與徽章、猜你喜歡
- **店家**：主控台（近 7 天圖表）、POS 收銀模式、優惠券與集點卡管理、店頭立牌 QR Code、店家照片
- **平台管理**：店家審核與精選、活動檔期、使用者管理、成長漏斗報表
- **無障礙**：大字模式、互動導覽、鍵盤操作，主要頁面通過 axe-core（WCAG 2.1 AA）自動檢測

## 技術
ASP.NET MVC 5（.NET Framework 4.8）、Entity Framework 6 Code First、Bootstrap 5、Leaflet＋OpenStreetMap，部署目標為 Microsoft Azure。

## 在自己電腦上執行
1. 安裝 Visual Studio（勾選「ASP.NET 與網頁程式開發」）。
2. Clone 這個專案，用 Visual Studio 開啟 `Smile.slnx`。第一次開啟時 Visual Studio 會自動還原 NuGet 套件（若沒有，請在方案上按右鍵選「還原 NuGet 套件」）。
3. 按 F5。第一次執行會在 LocalDB 自動建立資料庫並匯入示範資料（約 10 秒）。

示範帳號、資料來源與更多說明見 [Smile/README_學區生活圈.md](Smile/README_學區生活圈.md)；部署步驟見 [Smile/AZURE部署.md](Smile/AZURE部署.md)。

## 資料與授權說明
- 真實店家資料來自 © OpenStreetMap 貢獻者（ODbL）。
- 標示「示範」的店家、優惠、評論為虛構；示範店家照片來自 Unsplash（來源見 `Smile/Content/img/photos/CREDITS.txt`）。
- 吉祥物「小圈」與圖示為團隊自行繪製。
