using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Smile.DAL;
using Smile.Models;

namespace Smile.Infrastructure
{
    public class PrizeDef
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int Xp { get; set; }
        public int Weight { get; set; }      // 機率權重（總和 100 = 百分比）
        public string Color { get; set; }
        public bool Rare { get; set; }
    }

    /// <summary>
    /// 每日簽到的扭蛋。免費、每天一次、獎品只有經驗值和「幸運星」徽章，
    /// 不能換錢或折抵消費，所以不涉及金流，也不是付費抽獎。機率公開寫在簽到頁上。
    /// </summary>
    public static class Gacha
    {
        public static readonly PrizeDef[] Prizes =
        {
            new PrizeDef { Key = "xp5", Name = "經驗值 +5", Xp = 5, Weight = 40, Color = "#94A3B8" },
            new PrizeDef { Key = "xp10", Name = "經驗值 +10", Xp = 10, Weight = 30, Color = "#22C55E" },
            new PrizeDef { Key = "xp20", Name = "經驗值 +20", Xp = 20, Weight = 18, Color = "#3B82F6" },
            new PrizeDef { Key = "xp50", Name = "經驗值 +50", Xp = 50, Weight = 9, Color = "#A855F7" },
            new PrizeDef { Key = "star", Name = "大獎！幸運星", Xp = 100, Weight = 3, Color = "#F59E0B", Rare = true },
        };

        /// <summary>連續第 7、14、21… 天多送 30 點</summary>
        public const int StreakBonus = 30;

        public static PrizeDef Find(string key) { return Prizes.FirstOrDefault(p => p.Key == key) ?? Prizes[0]; }

        /// <summary>用密碼學亂數抽，避免被預測</summary>
        public static PrizeDef Draw()
        {
            var total = Prizes.Sum(p => p.Weight);
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var roll = (int)(BitConverter.ToUInt32(bytes, 0) % (uint)total);
            foreach (var p in Prizes)
            {
                if (roll < p.Weight) return p;
                roll -= p.Weight;
            }
            return Prizes[0];
        }

        /// <summary>連續簽到天數：從今天（或昨天，今天還沒簽也不算斷）往回數</summary>
        public static int Streak(IEnumerable<DateTime> days, DateTime today)
        {
            var set = new HashSet<DateTime>(days.Select(d => d.Date));
            var d0 = set.Contains(today) ? today : today.AddDays(-1);
            var n = 0;
            while (set.Contains(d0.AddDays(-n))) n++;
            return n;
        }

        public static int Streak(SmileContext db, int uid, DateTime today)
        {
            var since = today.AddDays(-400);
            return Streak(db.CheckIns.Where(c => c.UserId == uid && c.Day >= since).Select(c => c.Day).ToList(), today);
        }
    }
}
