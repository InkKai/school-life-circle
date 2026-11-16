namespace Smile.Models
{
    public static class AppRoles
    {
        public const string Member = "Member";
        public const string Merchant = "Merchant";
        public const string Admin = "Admin";

        public static string Display(string role)
        {
            switch (role)
            {
                case Merchant: return "店家";
                case Admin: return "管理者";
                default: return "會員";
            }
        }
    }

    public static class StoreStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static string Display(string status)
        {
            switch (status)
            {
                case Approved: return "已上架";
                case Rejected: return "已退回";
                default: return "待審核";
            }
        }
    }

    public static class CouponStatus
    {
        public const string Available = "Available";
        public const string Used = "Used";
    }

    public static class DataSources
    {
        public const string Merchant = "Merchant";
        public const string Osm = "OSM";
        public const string Demo = "Demo";
    }

    /// <summary>活動主題色：代碼與顯示名稱（漸層顏色在 Ui.ThemeGradient）</summary>
    public static class CampaignThemes
    {
        public static readonly string[][] All = {
            new[] { "indigo", "夜讀藍" }, new[] { "rose", "莓果紅" }, new[] { "teal", "青草綠" }, new[] { "amber", "暖陽棕" }
        };
    }

    public static class StampKinds
    {
        public const string Stamp = "Stamp";
        public const string Reward = "Reward";
    }
}
