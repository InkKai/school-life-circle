using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    public class HomeController : BaseController
    {
        // 首頁：小圈問候、今日優惠、活動、精選店家、本週熱門、附近店家
        public ActionResult Index(int? schoolId, int? tour)
        {
            // 剛註冊完，或從「我的」按重看導覽，就跑新手導覽
            ViewBag.StartTour = TempData["StartTour"] != null || tour == 1;

            // 第一次來（沒登入、也還沒選「先逛逛」）先到註冊頁
            if (!Request.IsAuthenticated && Request.Cookies["welcomed"] == null)
                return RedirectToAction("Register", "Account");

            var schools = db.Schools.OrderBy(s => s.Id).ToList();
            var origin = schools.FirstOrDefault(s => s.Id == schoolId) ?? schools.FirstOrDefault();
            var now = DateTime.Now;
            var today = now.Date;
            var uid = CurrentUserId;

            var model = new HomeViewModel
            {
                Schools = schools,
                Origin = origin,
                Categories = db.StoreCategories.OrderBy(c => c.SortOrder).ToList()
            };

            string name = null;
            if (uid != null)
            {
                name = db.Users.Where(u => u.Id == uid).Select(u => u.DisplayName).FirstOrDefault();

                // 快到期提醒：自己手上 3 天內到期、還沒用的券
                var soon = today.AddDays(3);
                ViewBag.Expiring = db.UserCoupons.Count(u => u.UserId == uid && u.Status == CouponStatus.Available && u.Coupon.EndDate >= today && u.Coupon.EndDate <= soon);

                // 新手任務：三件事還沒做完才顯示
                var stats = SyncBadges();
                if (stats.QuestDone < 3) model.Quest = stats;
                ViewBag.CheckedToday = db.CheckIns.Any(c => c.UserId == uid && c.Day == today);
                ViewBag.Streak = stats.Streak;
            }
            FillGreeting(model, now.Hour, name);

            model.FeaturedCoupons = db.Coupons.Include("Store").Include("Store.Category").Include("Campaign")
                .Where(c => c.IsActive && c.Store.Status == StoreStatus.Approved && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .OrderBy(c => c.EndDate).Take(6).ToList();

            var stores = db.Stores.Include("Category").Where(s => s.Status == StoreStatus.Approved).ToList();
            var activeCounts = db.Coupons.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today)
                .GroupBy(c => c.StoreId).ToDictionary(g => g.Key, g => g.Count());
            Func<Store, StoreListItem> toItem = s => new StoreListItem
            {
                Store = s,
                DistanceMeters = origin == null ? (double?)null : Geo.DistanceMeters(origin.Latitude, origin.Longitude, s.Latitude, s.Longitude),
                ActiveCoupons = activeCounts.ContainsKey(s.Id) ? activeCounts[s.Id] : 0
            };
            model.NearbyStores = stores.Select(toItem).OrderBy(i => i.DistanceMeters).Take(8).ToList();
            model.FeaturedStores = stores.Where(s => s.IsFeatured).Select(toItem).OrderBy(i => i.DistanceMeters).Take(8).ToList();
            model.StoreCount = stores.Count;
            model.CategoryCounts = stores.GroupBy(s => s.CategoryId).ToDictionary(g => g.Key, g => g.Count());

            // 進行中的活動（快結束的排前面）
            var campaign = db.Campaigns.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today).OrderBy(c => c.EndDate).FirstOrDefault();
            if (campaign != null)
            {
                model.Campaign = campaign;
                model.CampaignCoupons = db.Coupons.Count(c => c.CampaignId == campaign.Id && c.IsActive && c.Store.Status == StoreStatus.Approved && c.StartDate <= today && c.EndDate >= today);
            }

            // 大家都在領：最近 7 天被領最多次、而且現在還領得到的券（數字是實際紀錄，不灌水）
            var weekAgo = now.AddDays(-7);
            var hot = db.UserCoupons.Where(u => u.ClaimedAt >= weekAgo).GroupBy(u => u.CouponId)
                .Select(g => new { Id = g.Key, N = g.Count() }).OrderByDescending(x => x.N).Take(6).ToList();
            var hotIds = hot.Select(h => h.Id).ToList();
            var hotCoupons = db.Coupons.Include("Store").Include("Store.Category")
                .Where(c => hotIds.Contains(c.Id) && c.IsActive && c.Store.Status == StoreStatus.Approved && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .ToList();
            model.Trending = hot.Where(h => hotCoupons.Any(c => c.Id == h.Id)).Take(3)
                .Select(h => new TrendingItem { Coupon = hotCoupons.First(c => c.Id == h.Id), WeekClaims = h.N }).ToList();

            return View(model);
        }

        /// <summary>
        /// 猜你喜歡：滑到首頁最下面時才載入（每次 6 間，最多 3 次）。
        /// 有登入：依收藏和領過的券推算喜歡的分類；沒登入：推薦大家最常收藏、領券的店。
        /// </summary>
        public ActionResult ForYou(int? schoolId, int page = 1)
        {
            const int size = 6, maxPage = 3;
            page = Math.Max(1, Math.Min(page, maxPage));
            var school = db.Schools.FirstOrDefault(s => s.Id == schoolId) ?? db.Schools.OrderBy(s => s.Id).First();
            var today = DateTime.Today;
            var uid = CurrentUserId;

            var stores = db.Stores.Include("Category").Where(s => s.Status == StoreStatus.Approved).ToList();
            var activeCounts = db.Coupons.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .GroupBy(c => c.StoreId).ToDictionary(g => g.Key, g => g.Count());
            var popularity = db.Favorites.GroupBy(f => f.StoreId).ToDictionary(g => g.Key, g => g.Count());
            foreach (var g in db.UserCoupons.GroupBy(u => u.Coupon.StoreId).Select(g => new { g.Key, N = g.Count() }).ToList())
                popularity[g.Key] = (popularity.ContainsKey(g.Key) ? popularity[g.Key] : 0) + g.N;

            // 依喜好給分類加權
            var catScore = new Dictionary<int, int>();
            var seen = new HashSet<int>();
            string headline = "小圈覺得你會喜歡";
            if (uid != null)
            {
                var favStores = db.Favorites.Where(f => f.UserId == uid).Select(f => f.StoreId).ToList();
                var claimedStores = db.UserCoupons.Where(u => u.UserId == uid || u.GiftedFromId == uid).Select(u => u.Coupon.StoreId).ToList();
                foreach (var id in favStores) seen.Add(id);
                foreach (var s in stores.Where(s => favStores.Contains(s.Id))) catScore[s.CategoryId] = (catScore.ContainsKey(s.CategoryId) ? catScore[s.CategoryId] : 0) + 3;
                foreach (var id in claimedStores)
                {
                    var s = stores.FirstOrDefault(x => x.Id == id);
                    if (s != null) catScore[s.CategoryId] = (catScore.ContainsKey(s.CategoryId) ? catScore[s.CategoryId] : 0) + 2;
                }
            }
            var topCat = catScore.OrderByDescending(kv => kv.Value).Select(kv => (int?)kv.Key).FirstOrDefault();
            var topCatName = topCat.HasValue ? stores.First(s => s.CategoryId == topCat.Value).Category.Name : null;
            if (topCatName != null) headline = "你好像很喜歡" + topCatName + "，小圈再推薦幾間";

            var ranked = stores.Where(s => !seen.Contains(s.Id)).Select(s =>
            {
                var dist = Geo.DistanceMeters(school.Latitude, school.Longitude, s.Latitude, s.Longitude);
                var coupons = activeCounts.ContainsKey(s.Id) ? activeCounts[s.Id] : 0;
                var pop = popularity.ContainsKey(s.Id) ? popularity[s.Id] : 0;
                var cat = catScore.ContainsKey(s.CategoryId) ? catScore[s.CategoryId] : 0;
                // 分數：喜歡的分類 > 有優惠 > 熱門 > 近，太遠（超過 2 公里）不推
                var score = cat * 10 + coupons * 12 + pop * 4 + (s.PhotoUrl != null ? 3 : 0) - dist / 150.0;
                string reason;
                if (cat > 0 && s.CategoryId == topCat) reason = "你常逛" + s.Category.Name;
                else if (coupons > 0) reason = "現在有 " + coupons + " 張優惠";
                else if (pop > 0) reason = pop + " 人收藏或領過券";
                else reason = "走路 " + Geo.WalkMinutes(dist) + " 分鐘";
                return new { s, dist, coupons, score, reason };
            })
            .Where(x => x.dist <= 2000)
            .OrderByDescending(x => x.score).ThenBy(x => x.dist)
            .ToList();

            var pageItems = ranked.Skip((page - 1) * size).Take(size).Select(x => new ForYouItem
            {
                Item = new StoreListItem { Store = x.s, DistanceMeters = x.dist, ActiveCoupons = x.coupons },
                Reason = x.reason
            }).ToList();

            return PartialView("_ForYou", new ForYouViewModel
            {
                Items = pageItems,
                Headline = headline,
                Page = page,
                HasMore = page < maxPage && ranked.Count > page * size
            });
        }

        /// <summary>小圈依時段打招呼，順便建議現在適合找什麼</summary>
        private static void FillGreeting(HomeViewModel m, int hour, string name)
        {
            string hi, text, cta, kind = "food";
            if (hour >= 5 && hour < 10) { hi = "早安"; text = "吃早餐了嗎？"; cta = "幫我選早餐"; }
            else if (hour >= 10 && hour < 14) { hi = "午安"; text = "午餐吃什麼？"; cta = "幫我選午餐"; }
            else if (hour >= 14 && hour < 17) { hi = "下午好"; text = "來杯飲料休息一下？"; cta = "幫我選飲料"; kind = "drink"; }
            else if (hour >= 17 && hour < 21) { hi = "晚上好"; text = "晚餐吃什麼？"; cta = "幫我選晚餐"; }
            else { hi = "夜深了"; text = "吃點宵夜，還是早點休息？"; cta = "幫我選宵夜"; }

            m.GreetingTitle = name == null ? hi + "，我是小圈！" : hi + "，" + name + "！";
            m.GreetingText = text;
            m.GreetingCta = cta;
            m.GreetingKind = kind;
            m.MascotPose = (hour >= 23 || hour < 5) ? "sleep" : "wave";
        }

        // 「先逛逛」：記住已看過歡迎頁，之後直接進首頁
        public ActionResult Browse()
        {
            Response.Cookies.Add(new HttpCookie("welcomed", "1") { Expires = DateTime.Now.AddYears(1), HttpOnly = true });
            return RedirectToAction("Index");
        }

        // 使用教學（給長輩與第一次使用的人）
        public ActionResult Tutorial()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        // 店家合作：給老闆看的介紹頁（零抽成、怎麼上架、方案）
        public ActionResult Partner()
        {
            ViewBag.StoreCount = db.Stores.Count(s => s.Status == StoreStatus.Approved);
            return View();
        }

        // 大字模式切換：存在 Cookie，不登入也能用
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleTextSize(string returnUrl)
        {
            var current = Request.Cookies["textsize"];
            var large = current == null || current.Value != "large";
            Response.Cookies.Add(new HttpCookie("textsize", large ? "large" : "normal") { Expires = DateTime.Now.AddYears(1), HttpOnly = true });
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }
    }
}
