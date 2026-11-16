using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    /// <summary>會員卡：出示給店員掃描集點，並顯示手機條碼載具</summary>
    [Authorize]
    public class MemberCardController : BaseController
    {
        public ActionResult Index()
        {
            var uid = CurrentUserId.Value;
            var user = db.Users.Find(uid);
            if (user == null) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(user.MemberCode)) { user.MemberCode = NewUniqueMemberCode(); db.SaveChanges(); }

            var cards = db.StampCards.Include("Store.Category").Where(c => c.UserId == uid)
                .OrderByDescending(c => c.UpdatedAt).ToList();
            var joined = cards.Select(c => c.StoreId).ToList();
            var stampStores = db.Stores.Include("Category")
                .Where(s => s.StampGoal > 0 && s.Status == StoreStatus.Approved && !joined.Contains(s.Id))
                .OrderBy(s => s.Name).Take(12).ToList();
            // 等級與徽章
            var stats = SyncBadges();
            var earnedAt = db.UserBadges.Where(b => b.UserId == uid).ToDictionary(b => b.BadgeKey, b => b.EarnedAt);
            var earned = new HashSet<string>(earnedAt.Keys);
            return View(new MemberCardViewModel
            {
                User = user,
                Cards = cards,
                StampStores = stampStores,
                Level = Progress.Level(Progress.Xp(stats, earned.Count)),
                Badges = Progress.Badges.Select(b => new BadgeView { Def = b, Earned = earned.Contains(b.Key), EarnedAt = earned.Contains(b.Key) ? earnedAt[b.Key] : (DateTime?)null })
                    .OrderByDescending(b => b.Earned).ThenByDescending(b => b.EarnedAt).ToList()
            });
        }
    }
}
