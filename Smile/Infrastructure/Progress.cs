using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Smile.DAL;
using Smile.Models;

namespace Smile.Infrastructure
{
    /// <summary>使用者在平台上累積的行為次數（每次都從資料庫即時算，不另外存）</summary>
    public class UserStats
    {
        public int Claims { get; set; }       // 領過幾張券（含後來送給好友的）
        public int Redemptions { get; set; }  // 到店用掉幾張
        public int Favorites { get; set; }    // 收藏幾間店
        public int Friends { get; set; }      // 幾位好友
        public int Gifts { get; set; }        // 送給好友幾張券
        public int Reviews { get; set; }      // 寫過幾則評論
        public int Stamps { get; set; }       // 總共被蓋過幾點
        public int Rewards { get; set; }      // 集滿兌換幾次
        public int Categories { get; set; }   // 領過幾種分類的券
        public int Referrals { get; set; }    // 邀請幾位朋友註冊
        public int CheckInXp { get; set; }    // 簽到抽到的經驗值總和
        public int CheckIns { get; set; }     // 簽到天數
        public int Streak { get; set; }       // 目前連續簽到天數
        public int LuckyStars { get; set; }   // 抽中大獎次數

        // 新手任務：三件事做完就畢業
        public bool QuestClaim { get { return Claims > 0; } }
        public bool QuestFavorite { get { return Favorites > 0; } }
        public bool QuestFriend { get { return Friends > 0; } }
        public int QuestDone { get { return (QuestClaim ? 1 : 0) + (QuestFavorite ? 1 : 0) + (QuestFriend ? 1 : 0); } }
    }

    /// <summary>徽章的定義：名稱、怎麼拿到、圖示、顏色、達成條件</summary>
    public class BadgeDef
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Hint { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public Func<UserStats, bool> Earned { get; set; }
    }

    public class LevelInfo
    {
        public int Level { get; set; }
        public string Title { get; set; }
        public int Xp { get; set; }
        public int? NextXp { get; set; }
        public string NextTitle { get; set; }
        public int Percent { get; set; }
    }

    /// <summary>
    /// 等級與徽章：把「領券、到店、收藏、交朋友、集點、邀請」這些主要行為換成經驗值，
    /// 讓使用者有理由再回來。全部跟金錢無關，平台不發點數、不經手金流。
    /// </summary>
    public static class Progress
    {
        public static readonly BadgeDef[] Badges =
        {
            new BadgeDef { Key = "first_claim", Name = "第一張券", Hint = "領取任何一張優惠券", Icon = "ticket", Color = "#C2410C", Earned = s => s.Claims >= 1 },
            new BadgeDef { Key = "first_visit", Name = "到店達人", Hint = "到店使用一張券", Icon = "store", Color = "#0F766E", Earned = s => s.Redemptions >= 1 },
            new BadgeDef { Key = "collector", Name = "收藏家", Hint = "收藏 3 間店", Icon = "heart", Color = "#BE123C", Earned = s => s.Favorites >= 3 },
            new BadgeDef { Key = "friend", Name = "有伴真好", Hint = "加一位好友", Icon = "users", Color = "#4338CA", Earned = s => s.Friends >= 1 },
            new BadgeDef { Key = "gift", Name = "好康道相報", Hint = "送一張券給好友", Icon = "gift", Color = "#B45309", Earned = s => s.Gifts >= 1 },
            new BadgeDef { Key = "review", Name = "真心話", Hint = "寫一則店家評論", Icon = "pen", Color = "#6D28D9", Earned = s => s.Reviews >= 1 },
            new BadgeDef { Key = "stamp", Name = "集點新手", Hint = "在任何一間店集到點數", Icon = "star", Color = "#A16207", Earned = s => s.Stamps >= 1 },
            new BadgeDef { Key = "regular", Name = "老主顧", Hint = "集滿一張集點卡並兌換", Icon = "trophy", Color = "#9A3412", Earned = s => s.Rewards >= 1 },
            new BadgeDef { Key = "explorer", Name = "分類探險家", Hint = "領過 3 種分類的券", Icon = "compass", Color = "#0369A1", Earned = s => s.Categories >= 3 },
            new BadgeDef { Key = "inviter", Name = "呼朋引伴", Hint = "邀請一位朋友註冊", Icon = "share", Color = "#15803D", Earned = s => s.Referrals >= 1 },
            new BadgeDef { Key = "checkin", Name = "早安打卡", Hint = "第一次每日簽到", Icon = "cal", Color = "#0E7490", Earned = s => s.CheckIns >= 1 },
            new BadgeDef { Key = "streak7", Name = "全勤一週", Hint = "連續簽到 7 天", Icon = "fire", Color = "#DC2626", Earned = s => s.Streak >= 7 },
            new BadgeDef { Key = "lucky", Name = "幸運星", Hint = "在扭蛋抽中大獎", Icon = "star", Color = "#CA8A04", Earned = s => s.LuckyStars >= 1 },
            new BadgeDef { Key = "graduate", Name = "新手畢業", Hint = "完成三個新手任務", Icon = "book", Color = "#1F2937", Earned = s => s.QuestDone >= 3 },
        };

        public static readonly string[] LevelTitles = { "新生", "探險家", "在地人", "商圈達人", "生活圈大使" };
        private static readonly int[] LevelXp = { 0, 60, 180, 400, 800 };

        public static UserStats GetStats(SmileContext db, int uid)
        {
            var s = new UserStats
            {
                Gifts = db.UserCoupons.Count(u => u.GiftedFromId == uid),
                Redemptions = db.UserCoupons.Count(u => u.UserId == uid && u.Status == CouponStatus.Used),
                Favorites = db.Favorites.Count(f => f.UserId == uid),
                Friends = db.Friendships.Count(f => f.UserId == uid),
                Reviews = db.Reviews.Count(r => r.UserId == uid),
                Stamps = db.StampRecords.Where(r => r.StampCard.UserId == uid && r.Kind == StampKinds.Stamp).Select(r => (int?)r.Change).Sum() ?? 0,
                Rewards = db.StampRecords.Count(r => r.StampCard.UserId == uid && r.Kind == StampKinds.Reward),
                Referrals = db.Users.Count(u => u.ReferredById == uid),
                CheckIns = db.CheckIns.Count(c => c.UserId == uid),
                CheckInXp = db.CheckIns.Where(c => c.UserId == uid).Select(c => (int?)c.Xp).Sum() ?? 0,
                LuckyStars = db.CheckIns.Count(c => c.UserId == uid && c.Prize == "star"),
                Streak = Gacha.Streak(db, uid, DateTime.Today),
                Categories = db.UserCoupons.Where(u => (u.UserId == uid && u.GiftedFromId == null) || u.GiftedFromId == uid)
                    .Select(u => u.Coupon.Store.CategoryId).Distinct().Count()
            };
            s.Claims = db.UserCoupons.Count(u => u.UserId == uid && u.GiftedFromId == null) + s.Gifts;
            return s;
        }

        /// <summary>經驗值：到店使用和邀請朋友給最多，因為這兩件事對店家和平台最有價值</summary>
        public static int Xp(UserStats s, int badgeCount)
        {
            return s.Claims * 10 + s.Redemptions * 30 + s.Favorites * 5 + s.Friends * 10 + s.Gifts * 10
                 + s.Reviews * 15 + s.Stamps * 5 + s.Rewards * 40 + s.Referrals * 50 + s.CheckInXp + badgeCount * 20;
        }

        public static LevelInfo Level(int xp)
        {
            var i = 0;
            while (i + 1 < LevelXp.Length && xp >= LevelXp[i + 1]) i++;
            var info = new LevelInfo { Level = i + 1, Title = LevelTitles[i], Xp = xp, Percent = 100 };
            if (i + 1 < LevelXp.Length)
            {
                info.NextXp = LevelXp[i + 1];
                info.NextTitle = LevelTitles[i + 1];
                info.Percent = (int)Math.Round((xp - LevelXp[i]) * 100.0 / (LevelXp[i + 1] - LevelXp[i]));
            }
            return info;
        }

        /// <summary>把新達成的徽章寫進資料庫，回傳這次新解鎖的徽章</summary>
        public static List<BadgeDef> Sync(SmileContext db, int uid, UserStats stats)
        {
            var have = new HashSet<string>(db.UserBadges.Where(b => b.UserId == uid).Select(b => b.BadgeKey));
            var fresh = Badges.Where(b => !have.Contains(b.Key) && b.Earned(stats)).ToList();
            if (fresh.Count == 0) return fresh;

            var now = DateTime.Now;
            foreach (var b in fresh) db.UserBadges.Add(new UserBadge { UserId = uid, BadgeKey = b.Key, EarnedAt = now });
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // 同時開兩個分頁時可能重複寫入，唯一索引會擋下來；放掉這次新增即可
                foreach (var e in db.ChangeTracker.Entries<UserBadge>().Where(x => x.State == EntityState.Added).ToList())
                    e.State = EntityState.Detached;
                return new List<BadgeDef>();
            }
            return fresh;
        }
    }
}
