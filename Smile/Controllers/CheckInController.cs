using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    /// <summary>每日簽到抽抽樂</summary>
    [Authorize]
    public class CheckInController : BaseController
    {
        public ActionResult Index()
        {
            var uid = CurrentUserId.Value;
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var records = db.CheckIns.Where(c => c.UserId == uid && c.Day >= monthStart).ToList();
            var stats = SyncBadges();

            var model = new CheckInViewModel
            {
                Today = today,
                MonthStart = monthStart,
                Days = records.ToDictionary(c => c.Day.Date, c => c.Prize),
                CheckedToday = records.Any(c => c.Day == today),
                Streak = stats.Streak,
                TotalDays = stats.CheckIns,
                TotalXp = stats.CheckInXp,
                LuckyStars = stats.LuckyStars
            };

            // 剛抽完：顯示扭蛋開出來的結果
            var key = TempData["Prize"] as string;
            if (key != null)
            {
                model.JustDrew = Gacha.Find(key);
                model.JustXp = TempData["PrizeXp"] as int? ?? model.JustDrew.Xp;
                model.JustBonus = TempData["StreakBonus"] != null;
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Draw()
        {
            var uid = CurrentUserId.Value;
            var today = DateTime.Today;
            if (db.CheckIns.Any(c => c.UserId == uid && c.Day == today))
            {
                Flash("今天已經簽到過了，明天再來！", "info");
                return RedirectToAction("Index");
            }

            var prize = Gacha.Draw();
            // 連續天數：昨天往回數，再加上今天
            var streak = Gacha.Streak(db, uid, today.AddDays(-1)) + 1;
            var bonus = streak % 7 == 0;
            var xp = prize.Xp + (bonus ? Gacha.StreakBonus : 0);
            db.CheckIns.Add(new CheckIn { UserId = uid, Day = today, Prize = prize.Key, Xp = xp, CreatedAt = DateTime.Now });
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // 同時按兩次：唯一索引擋下第二筆
                Flash("今天已經簽到過了，明天再來！", "info");
                return RedirectToAction("Index");
            }
            TempData["Prize"] = prize.Key;
            TempData["PrizeXp"] = xp;
            if (bonus) TempData["StreakBonus"] = true;
            return RedirectToAction("Index");
        }
    }
}
