using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Smile.Infrastructure;
using Smile.Models;

namespace Smile.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "請輸入電子郵件"), EmailAddress(ErrorMessage = "電子郵件格式不正確"), StringLength(100)]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入顯示名稱"), StringLength(50)]
        [Display(Name = "顯示名稱")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "請輸入密碼"), StringLength(100, MinimumLength = 8, ErrorMessage = "密碼至少 8 個字元")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "兩次輸入的密碼不同")]
        [DataType(DataType.Password)]
        [Display(Name = "再輸入一次密碼")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "我是店家，要申請上架")]
        public bool IsMerchant { get; set; }

        [Display(Name = "學校（可不填）")]
        public int? SchoolId { get; set; }

        /// <summary>邀請連結帶來的邀請碼</summary>
        public string Ref { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "請輸入電子郵件"), EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Display(Name = "保持登入")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class ProfileViewModel
    {
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        [Display(Name = "角色")]
        public string RoleName { get; set; }

        [Required(ErrorMessage = "請輸入顯示名稱"), StringLength(50)]
        [Display(Name = "顯示名稱")]
        public string DisplayName { get; set; }

        [Display(Name = "學校")]
        public int? SchoolId { get; set; }

        [Display(Name = "預設使用大字模式")]
        public bool PreferLargeText { get; set; }

        [StringLength(8)]
        [Display(Name = "手機條碼載具（可不填）")]
        public string CarrierCode { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "目前密碼（要改密碼才需填）")]
        public string CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "新密碼至少 8 個字元")]
        [DataType(DataType.Password)]
        [Display(Name = "新密碼")]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "兩次輸入的新密碼不同")]
        [DataType(DataType.Password)]
        [Display(Name = "再輸入一次新密碼")]
        public string ConfirmNewPassword { get; set; }
    }

    public class StoreListItem
    {
        public Store Store { get; set; }
        public double? DistanceMeters { get; set; }
        public int ActiveCoupons { get; set; }
        public double? AverageRating { get; set; }
    }

    public class StoreIndexViewModel
    {
        public List<StoreListItem> Items { get; set; }
        public List<StoreCategory> Categories { get; set; }
        public List<School> Schools { get; set; }
        public int? CategoryId { get; set; }
        public int? SchoolId { get; set; }
        public string Query { get; set; }
        public bool OnlyWithCoupons { get; set; }
        public bool OnlyAccessible { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public string OriginName { get; set; }
        public int Page { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }

    public class StoreDetailsViewModel
    {
        public Store Store { get; set; }
        public List<Product> Products { get; set; }
        public List<Coupon> Coupons { get; set; }
        public List<Review> Reviews { get; set; }
        public HashSet<int> ClaimedCouponIds { get; set; }
        public bool IsFavorite { get; set; }
        public double? AverageRating { get; set; }
        public bool CanReview { get; set; }

        // 連續逛店：上一家（剛剛看過的）、下一家（附近還沒看過的）、附近還有
        public Store PrevStore { get; set; }
        public StoreListItem NextStore { get; set; }
        public List<StoreListItem> Nearby { get; set; }
    }

    public class HomeViewModel
    {
        public List<Coupon> FeaturedCoupons { get; set; }
        public List<StoreListItem> NearbyStores { get; set; }
        public List<StoreCategory> Categories { get; set; }
        public List<School> Schools { get; set; }
        public School Origin { get; set; }
        public int StoreCount { get; set; }
        public Dictionary<int, int> CategoryCounts { get; set; }

        // 小圈的問候（依時段）
        public string GreetingTitle { get; set; }
        public string GreetingText { get; set; }
        public string GreetingCta { get; set; }
        public string GreetingKind { get; set; }
        public string MascotPose { get; set; }

        /// <summary>新手任務進度；沒登入或已經畢業時為 null</summary>
        public UserStats Quest { get; set; }

        public Campaign Campaign { get; set; }
        public int CampaignCoupons { get; set; }
        public List<StoreListItem> FeaturedStores { get; set; }
        public List<TrendingItem> Trending { get; set; }
    }

    /// <summary>本週熱門優惠：券＋最近 7 天有幾個人領</summary>
    public class TrendingItem
    {
        public Coupon Coupon { get; set; }
        public int WeekClaims { get; set; }
    }

    public class MerchantDashboardViewModel
    {
        public List<Store> Stores { get; set; }
        public Store CurrentStore { get; set; }
        public int TodayRedemptions { get; set; }
        public int ActiveCoupons { get; set; }
        public int MonthClaims { get; set; }
        public List<Redemption> RecentRedemptions { get; set; }
        public List<DayStat> Week { get; set; }
        public int Favorites { get; set; }
        public int StampCards { get; set; }
    }

    /// <summary>某一天的領券數與核銷數（店家主控台的近 7 天圖表）</summary>
    public class DayStat
    {
        public DateTime Date { get; set; }
        public int Claims { get; set; }
        public int Redemptions { get; set; }
    }

    public class CategoryReportRow
    {
        public string Category { get; set; }
        public int Stores { get; set; }
        public int Claims { get; set; }
        public int Redemptions { get; set; }
    }

    public class ReportViewModel
    {
        public int Users { get; set; }
        public int Merchants { get; set; }
        public int ApprovedStores { get; set; }
        public int PendingStores { get; set; }
        public int Claims { get; set; }
        public int Redemptions { get; set; }
        public List<CategoryReportRow> ByCategory { get; set; }

        // 成長漏斗（AARRR）
        public int Members { get; set; }
        public int ClaimedMembers { get; set; }
        public int RedeemedMembers { get; set; }
        public int ReturnedMembers { get; set; }
        public int ReferredMembers { get; set; }
        public int NewMembers7d { get; set; }
        public int FriendPairs { get; set; }
        public int StampsGiven { get; set; }
        public int FeaturedStores { get; set; }
        public int JoinedStores { get; set; }
    }

    /// <summary>今天吃什麼：小圈幫你隨機挑一間</summary>
    public class PickViewModel
    {
        public List<School> Schools { get; set; }
        public int SchoolId { get; set; }
        public int Minutes { get; set; }
        public bool OnlyCoupon { get; set; }
        public string Kind { get; set; }
        public int CandidateCount { get; set; }
        public List<string> RollNames { get; set; }
        public StoreListItem Result { get; set; }
        public bool Spun { get; set; }
    }

    /// <summary>會員卡頁：會員 QR Code、載具條碼、各店集點卡</summary>
    public class MemberCardViewModel
    {
        public User User { get; set; }
        public List<StampCard> Cards { get; set; }
        public List<Store> StampStores { get; set; }
        public LevelInfo Level { get; set; }
        public List<BadgeView> Badges { get; set; }
    }

    public class BadgeView
    {
        public BadgeDef Def { get; set; }
        public bool Earned { get; set; }
        public DateTime? EarnedAt { get; set; }
    }

    /// <summary>每日簽到頁</summary>
    public class CheckInViewModel
    {
        public DateTime Today { get; set; }
        public DateTime MonthStart { get; set; }
        public Dictionary<DateTime, string> Days { get; set; }
        public bool CheckedToday { get; set; }
        public int Streak { get; set; }
        public int TotalDays { get; set; }
        public int TotalXp { get; set; }
        public int LuckyStars { get; set; }
        public PrizeDef JustDrew { get; set; }
        public int JustXp { get; set; }
        public bool JustBonus { get; set; }
    }

    /// <summary>猜你喜歡：推薦的店＋推薦理由</summary>
    public class ForYouItem
    {
        public StoreListItem Item { get; set; }
        public string Reason { get; set; }
    }

    public class ForYouViewModel
    {
        public List<ForYouItem> Items { get; set; }
        public string Headline { get; set; }
        public int Page { get; set; }
        public bool HasMore { get; set; }
    }

    public class PosLogItem
    {
        public DateTime Time { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }
    }

    /// <summary>店家 POS 收銀模式</summary>
    public class PosViewModel
    {
        public Store Store { get; set; }
        public List<Store> Stores { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }
        public User Member { get; set; }
        public StampCard Card { get; set; }
        public int TodayRedemptions { get; set; }
        public int TodayStamps { get; set; }
        public int TodayRewards { get; set; }
        public List<PosLogItem> Log { get; set; }
    }

    /// <summary>店家集點卡設定頁</summary>
    public class StampSettingsViewModel
    {
        public int StoreId { get; set; }
        public Store Store { get; set; }

        [Range(0, 30, ErrorMessage = "集點數需介於 0 到 30")]
        [Display(Name = "集滿幾點可兌換（0 表示不開放集點）")]
        public int StampGoal { get; set; }

        [StringLength(50)]
        [Display(Name = "集滿可兌換什麼")]
        public string StampReward { get; set; }

        public int CardCount { get; set; }
        public int TotalStamps { get; set; }
        public int TotalRewards { get; set; }
    }
}
