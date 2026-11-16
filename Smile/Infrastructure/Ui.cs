using System;
using System.Web;
using Smile.Models;

namespace Smile.Infrastructure
{
    /// <summary>畫面用的小工具：分類圖示、顏色、外部地圖連結</summary>
    public static class Ui
    {
        public static string Icon(StoreCategory c) { return c != null && !string.IsNullOrEmpty(c.Icon) ? c.Icon : "🏪"; }

        // 依店名與料理類型挑更貼切的圖示（例如拉麵店用 🍜），讓卡片不會都長一樣
        private static readonly string[][] Keywords = {
            new[] { "拉麵", "🍜" }, new[] { "ラーメン", "🍜" }, new[] { "牛肉麵", "🍜" }, new[] { "麵", "🍜" }, new[] { "火鍋", "🍲" }, new[] { "鍋", "🍲" },
            new[] { "咖啡", "☕" }, new[] { "Coffee", "☕" }, new[] { "coffee", "☕" }, new[] { "Cafe", "☕" }, new[] { "cafe", "☕" }, new[] { "茶", "🍵" },
            new[] { "壽司", "🍣" }, new[] { "日式", "🍣" }, new[] { "漢堡", "🍔" }, new[] { "麥當勞", "🍔" }, new[] { "披薩", "🍕" }, new[] { "Pizza", "🍕" },
            new[] { "牛排", "🥩" }, new[] { "燒肉", "🥩" }, new[] { "燒烤", "🍢" }, new[] { "炸雞", "🍗" }, new[] { "雞", "🍗" }, new[] { "餃", "🥟" }, new[] { "包子", "🥟" },
            new[] { "早餐", "🍳" }, new[] { "早午餐", "🍳" }, new[] { "三明治", "🥪" }, new[] { "吐司", "🍞" }, new[] { "麵包", "🥐" }, new[] { "烘焙", "🥐" },
            new[] { "蛋糕", "🍰" }, new[] { "甜點", "🍰" }, new[] { "冰", "🍧" }, new[] { "豆花", "🍮" }, new[] { "飲", "🥤" }, new[] { "果汁", "🧃" },
            new[] { "咖哩", "🍛" }, new[] { "飯", "🍚" }, new[] { "便當", "🍱" }, new[] { "素", "🥗" }, new[] { "沙拉", "🥗" }, new[] { "韓", "🍲" }, new[] { "泰", "🍤" },
            new[] { "鞋", "👟" }, new[] { "包", "👜" }, new[] { "眼鏡", "👓" }, new[] { "藥", "💊" }, new[] { "妝", "💄" }, new[] { "美", "💄" }
        };

        public static string Icon(Store s)
        {
            if (s == null) return "🏪";
            var text = (s.Name ?? "") + " " + (s.Cuisine ?? "");
            foreach (var k in Keywords)
                if (text.Contains(k[0])) return k[1];
            return Icon(s.Category);
        }

        public static string Color(StoreCategory c) { return c != null && !string.IsNullOrEmpty(c.Color) ? c.Color : "#C2410C"; }

        /// <summary>分類色的淡色背景（加上透明度）</summary>
        public static string Tint(StoreCategory c) { return Color(c) + "22"; }

        /// <summary>Google 地圖搜尋連結（官方 Maps URLs，不需金鑰）</summary>
        public static string GoogleMapsUrl(Store s)
        {
            return "https://www.google.com/maps/search/?api=1&query=" + HttpUtility.UrlEncode(s.Name + " " + s.Latitude.ToString("0.000000") + "," + s.Longitude.ToString("0.000000"));
        }

        /// <summary>Google 街景連結（官方 Maps URLs，不需金鑰）</summary>
        public static string StreetViewUrl(Store s)
        {
            return "https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=" + s.Latitude.ToString("0.000000") + "," + s.Longitude.ToString("0.000000");
        }

        // ===== 自畫 SVG 圖示（Views/Shared/_Icons.cshtml）取代 emoji =====
        /// <summary>線條圖示，例如 Ui.Ic("pin")</summary>
        public static IHtmlString Ic(string name, string cls = "ic")
        {
            return new HtmlString("<svg class=\"" + cls + "\" aria-hidden=\"true\" focusable=\"false\"><use href=\"#i-" + name + "\"></use></svg>");
        }

        private static readonly string[][] ArtKeywords = {
            new[] { "拉麵", "noodle" }, new[] { "ラーメン", "noodle" }, new[] { "麵", "noodle" }, new[] { "咖啡", "coffee" }, new[] { "Coffee", "coffee" }, new[] { "coffee", "coffee" },
            new[] { "Cafe", "coffee" }, new[] { "cafe", "coffee" }, new[] { "漢堡", "burger" }, new[] { "麥當勞", "burger" }, new[] { "炸雞", "burger" }, new[] { "蛋糕", "cake" },
            new[] { "甜點", "cake" }, new[] { "麵包", "cake" }, new[] { "烘焙", "cake" }, new[] { "冰", "cake" }, new[] { "茶", "drink" }, new[] { "飲", "drink" }, new[] { "果汁", "drink" }
        };
        private static readonly System.Collections.Generic.Dictionary<string, string> CategoryArt = new System.Collections.Generic.Dictionary<string, string> {
            { "餐飲", "bento" }, { "飲料甜點", "drink" }, { "服飾", "shirt" }, { "鞋子", "shoe" }, { "飾品", "ring" }, { "藥妝", "pill" }, { "眼鏡", "glasses" }
        };
        public static string ArtName(StoreCategory c) { string n; return c != null && CategoryArt.TryGetValue(c.Name, out n) ? n : "bento"; }
        public static string ArtName(Store s)
        {
            if (s == null) return "bento";
            if (s.Category == null || s.Category.Name == "餐飲" || s.Category.Name == "飲料甜點")
            {
                var text = (s.Name ?? "") + " " + (s.Cuisine ?? "");
                foreach (var k in ArtKeywords) if (text.Contains(k[0])) return k[1];
            }
            return ArtName(s.Category);
        }
        public static IHtmlString ArtSvg(string name, string cls = "")
        {
            return new HtmlString("<svg class=\"" + cls + "\" aria-hidden=\"true\" focusable=\"false\" viewBox=\"0 0 48 48\"><use href=\"#a-" + name + "\"></use></svg>");
        }
        public static IHtmlString Art(Store s, string cls = "") { return ArtSvg(ArtName(s), cls); }
        public static IHtmlString Art(StoreCategory c, string cls = "") { return ArtSvg(ArtName(c), cls); }

        /// <summary>從優惠標題抓出最吸睛的字，例如「便當折 10 元」→「$10」、「全面 9 折」→「9折」</summary>
        public static string CouponHeadline(string title)
        {
            title = title ?? "";
            if (title.Contains("買一送一")) return "1+1";
            if (title.Contains("半價")) return "半價";
            if (title.Contains("免費")) return "免費";
            var m = System.Text.RegularExpressions.Regex.Match(title, @"折\s*(\d+)");
            if (m.Success) return "$" + m.Groups[1].Value;
            m = System.Text.RegularExpressions.Regex.Match(title, @"(\d+)\s*折");
            if (m.Success) return m.Groups[1].Value + "折";
            return "優惠";
        }

        // 輪播用的深色漸層（白字對比都在 4.5:1 以上）
        private static readonly string[] Gradients = {
            "linear-gradient(120deg,#9A3412,#C2410C 55%,#EA580C)", "linear-gradient(120deg,#312E81,#4338CA)",
            "linear-gradient(120deg,#134E4A,#0F766E)", "linear-gradient(120deg,#881337,#BE123C)", "linear-gradient(120deg,#4C1D95,#6D28D9)"
        };
        public static string Gradient(int i) { return Gradients[Math.Abs(i) % Gradients.Length]; }

        /// <summary>活動主題色的漸層（白字對比都在 4.5:1 以上）</summary>
        public static string ThemeGradient(string theme)
        {
            switch (theme)
            {
                case "rose": return "linear-gradient(120deg,#881337,#BE123C)";
                case "teal": return "linear-gradient(120deg,#134E4A,#0F766E)";
                case "amber": return "linear-gradient(120deg,#78350F,#B45309)";
                default: return "linear-gradient(120deg,#312E81,#4338CA)";
            }
        }

        public static string DataSourceLabel(Store s)
        {
            if (s.DataSource == DataSources.Osm) return "OpenStreetMap 店家資料";
            if (s.IsDemo) return "示範店家（虛構）";
            return "已加入平台";
        }
    }
}
