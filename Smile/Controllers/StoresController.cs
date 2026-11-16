using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    public class StoresController : BaseController
    {
        // 附近店家：依學校或目前位置算距離，可篩選分類、關鍵字、有優惠、無障礙
        public ActionResult Index(int? categoryId, int? schoolId, string q, bool withCoupons = false, bool accessible = false, double? lat = null, double? lng = null, int page = 1)
        {
            var schools = db.Schools.OrderBy(s => s.Id).ToList();
            double? oLat = lat, oLng = lng;
            string originName = "目前位置";
            if (oLat == null || oLng == null)
            {
                var school = schools.FirstOrDefault(s => s.Id == schoolId) ?? schools.FirstOrDefault();
                if (school != null) { oLat = school.Latitude; oLng = school.Longitude; originName = school.ShortName; schoolId = school.Id; }
            }

            var today = DateTime.Today;
            var query = db.Stores.Include("Category").Where(s => s.Status == StoreStatus.Approved);
            if (categoryId.HasValue) query = query.Where(s => s.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(s => s.Name.Contains(kw) || s.Description.Contains(kw) || s.Products.Any(p => p.Name.Contains(kw)));
            }
            if (accessible) query = query.Where(s => s.AccessibilityKnown && (s.StepFreeEntrance || s.HasRamp));

            var activeCounts = db.Coupons.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .GroupBy(c => c.StoreId).ToDictionary(g => g.Key, g => g.Count());
            var ratings = db.Reviews.GroupBy(r => r.StoreId).ToDictionary(g => g.Key, g => g.Average(r => (double)r.Rating));

            var items = query.ToList().Select(s => new StoreListItem
            {
                Store = s,
                DistanceMeters = oLat.HasValue ? Geo.DistanceMeters(oLat.Value, oLng.Value, s.Latitude, s.Longitude) : (double?)null,
                ActiveCoupons = activeCounts.ContainsKey(s.Id) ? activeCounts[s.Id] : 0,
                AverageRating = ratings.ContainsKey(s.Id) ? ratings[s.Id] : (double?)null
            });
            if (withCoupons) items = items.Where(i => i.ActiveCoupons > 0);
            // 服務範圍：兩公里內
            items = items.Where(i => !i.DistanceMeters.HasValue || i.DistanceMeters <= 2000);

            const int pageSize = 24;
            var ordered = items.OrderByDescending(i => i.ActiveCoupons > 0).ThenBy(i => i.DistanceMeters).ToList();
            page = Math.Max(1, page);
            return View(new StoreIndexViewModel
            {
                Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = ordered.Count,
                Page = page,
                PageSize = pageSize,
                Categories = db.StoreCategories.OrderBy(c => c.SortOrder).ToList(),
                Schools = schools,
                CategoryId = categoryId,
                SchoolId = lat.HasValue ? null : schoolId,
                Query = q,
                OnlyWithCoupons = withCoupons,
                OnlyAccessible = accessible,
                Lat = lat,
                Lng = lng,
                OriginName = originName
            });
        }

        private static readonly Random Rng = new Random();

        // 今天吃什麼：依步行時間、有沒有優惠、想吃正餐或飲料，隨機挑一間
        public ActionResult Pick(int? schoolId, int minutes = 10, bool coupon = false, string kind = "food", bool spin = false)
        {
            var schools = db.Schools.OrderBy(s => s.Id).ToList();
            var school = schools.FirstOrDefault(s => s.Id == schoolId) ?? schools.First();
            if (minutes != 5 && minutes != 10 && minutes != 15) minutes = 10;
            if (kind != "food" && kind != "drink") kind = "food";
            var catName = kind == "drink" ? "飲料甜點" : "餐飲";
            var today = DateTime.Today;
            var activeCounts = db.Coupons.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .GroupBy(c => c.StoreId).ToDictionary(g => g.Key, g => g.Count());

            var candidates = db.Stores.Include("Category").Where(s => s.Status == StoreStatus.Approved && s.Category.Name == catName).ToList()
                .Select(s => new StoreListItem
                {
                    Store = s,
                    DistanceMeters = Geo.DistanceMeters(school.Latitude, school.Longitude, s.Latitude, s.Longitude),
                    ActiveCoupons = activeCounts.ContainsKey(s.Id) ? activeCounts[s.Id] : 0
                })
                .Where(i => Geo.WalkMinutes(i.DistanceMeters.Value) <= minutes && (!coupon || i.ActiveCoupons > 0))
                .ToList();

            StoreListItem result = null;
            List<string> roll;
            lock (Rng)
            {
                if (spin && candidates.Any()) result = candidates[Rng.Next(candidates.Count)];
                roll = candidates.OrderBy(x => Rng.Next()).Take(14).Select(c => c.Store.Name).ToList();
            }
            return View(new PickViewModel
            {
                Schools = schools, SchoolId = school.Id, Minutes = minutes, OnlyCoupon = coupon, Kind = kind,
                CandidateCount = candidates.Count, RollNames = roll, Result = result, Spun = spin
            });
        }

        // 地圖模式
        public ActionResult Map()
        {
            ViewBag.Categories = db.StoreCategories.OrderBy(c => c.SortOrder).ToList();
            return View();
        }

        // 地圖用資料（JSON）
        public ActionResult MapData()
        {
            var today = DateTime.Today;
            var counts = db.Coupons.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .GroupBy(c => c.StoreId).ToDictionary(g => g.Key, g => g.Count());
            var data = db.Stores.Include("Category").Where(s => s.Status == StoreStatus.Approved).ToList()
                .Select(s => new
                {
                    s.Id, s.Name, lat = s.Latitude, lng = s.Longitude,
                    Category = s.Category.Name,
                    Icon = Ui.ArtName(s),
                    Coupons = counts.ContainsKey(s.Id) ? counts[s.Id] : 0,
                    url = Url.Action("Details", new { id = s.Id })
                });
            var schools = db.Schools.ToList().Select(s => new { s.ShortName, lat = s.Latitude, lng = s.Longitude });
            return Json(new { stores = data, schools }, JsonRequestBehavior.AllowGet);
        }

        // 店家詳情
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var store = db.Stores.Include("Category").FirstOrDefault(s => s.Id == id);
            // 未上架的店只有店主與管理者看得到
            if (store == null || (store.Status != StoreStatus.Approved && store.OwnerId != CurrentUserId && !User.IsInRole(AppRoles.Admin)))
                return HttpNotFound();

            var today = DateTime.Today;
            var uid = CurrentUserId;
            var reviews = db.Reviews.Include("User").Where(r => r.StoreId == store.Id).OrderByDescending(r => r.CreatedAt).ToList();
            SyncBadges();
            var nav = BuildStoreNav(store);
            // 分享到 LINE 時的預覽文字與圖片
            ViewBag.Description = store.OwnerMessage ?? (store.Category.Name + "・" + store.Address);
            ViewBag.OgImage = store.PhotoUrl;
            return View(new StoreDetailsViewModel
            {
                Store = store,
                Products = db.Products.Where(p => p.StoreId == store.Id && p.IsAvailable).OrderBy(p => p.Price).ToList(),
                Coupons = db.Coupons.Where(c => c.StoreId == store.Id && c.IsActive && c.StartDate <= today && c.EndDate >= today).OrderBy(c => c.EndDate).ToList(),
                Reviews = reviews,
                ClaimedCouponIds = uid == null ? new HashSet<int>() : new HashSet<int>(db.UserCoupons.Where(u => u.UserId == uid && u.Coupon.StoreId == store.Id).Select(u => u.CouponId)),
                IsFavorite = uid != null && db.Favorites.Any(f => f.UserId == uid && f.StoreId == store.Id),
                AverageRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : (double?)null,
                CanReview = uid != null && !reviews.Any(r => r.UserId == uid),
                PrevStore = nav.Item1,
                NextStore = nav.Item2,
                Nearby = nav.Item3
            });
        }

        /// <summary>
        /// 連續逛店：用 Session 記住這次逛過哪些店（最多 30 間）。
        /// 上一家＝剛剛看的那間；下一家＝離這間最近、這次還沒看過的店（同分類、有優惠的優先）。
        /// </summary>
        private Tuple<Store, StoreListItem, List<StoreListItem>> BuildStoreNav(Store store)
        {
            var trail = Session["storeTrail"] as List<int> ?? new List<int>();
            if (trail.Count >= 2 && trail[trail.Count - 2] == store.Id)
                trail.RemoveAt(trail.Count - 1);                  // 按「上一家」或瀏覽器返回
            else if (trail.Count == 0 || trail[trail.Count - 1] != store.Id)
                trail.Add(store.Id);                              // 逛到新的一間
            if (trail.Count > 30) trail.RemoveRange(0, trail.Count - 30);
            Session["storeTrail"] = trail;

            Store prev = null;
            if (trail.Count >= 2)
            {
                var prevId = trail[trail.Count - 2];
                prev = db.Stores.FirstOrDefault(s => s.Id == prevId && s.Status == StoreStatus.Approved);
            }

            var today = DateTime.Today;
            var activeCounts = db.Coupons.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .GroupBy(c => c.StoreId).ToDictionary(g => g.Key, g => g.Count());
            var seen = new HashSet<int>(trail);
            var near = db.Stores.Include("Category").Where(s => s.Status == StoreStatus.Approved && s.Id != store.Id).ToList()
                .Where(s => !seen.Contains(s.Id))
                .Select(s =>
                {
                    var d = Geo.DistanceMeters(store.Latitude, store.Longitude, s.Latitude, s.Longitude);
                    var coupons = activeCounts.ContainsKey(s.Id) ? activeCounts[s.Id] : 0;
                    // 排序用的「感覺距離」：同分類、有優惠、有照片的店感覺比較近
                    var feel = d * (s.CategoryId == store.CategoryId ? 0.6 : 1.0) * (coupons > 0 ? 0.5 : 1.0) * (s.PhotoUrl != null ? 0.8 : 1.0);
                    return new { Item = new StoreListItem { Store = s, DistanceMeters = d, ActiveCoupons = coupons }, feel, d };
                })
                .Where(x => x.d <= 1500)
                .OrderBy(x => x.feel)
                .Take(7)
                .Select(x => x.Item)
                .ToList();
            return Tuple.Create(prev, near.FirstOrDefault(), near.Skip(1).ToList());
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleFavorite(int id)
        {
            var uid = CurrentUserId.Value;
            var fav = db.Favorites.FirstOrDefault(f => f.UserId == uid && f.StoreId == id);
            if (fav != null) { db.Favorites.Remove(fav); Flash("已取消收藏"); }
            else if (db.Stores.Any(s => s.Id == id && s.Status == StoreStatus.Approved))
            {
                db.Favorites.Add(new Favorite { UserId = uid, StoreId = id, CreatedAt = DateTime.Now });
                Flash("已加入收藏");
            }
            db.SaveChanges();
            return RedirectToAction("Details", new { id });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddReview(int id, int rating, string comment)
        {
            var uid = CurrentUserId.Value;
            if (rating < 1 || rating > 5) { Flash("請選擇 1 到 5 顆星", "warning"); return RedirectToAction("Details", new { id }); }
            if (db.Reviews.Any(r => r.UserId == uid && r.StoreId == id)) { Flash("你已經評論過這間店了", "warning"); return RedirectToAction("Details", new { id }); }
            if (!db.Stores.Any(s => s.Id == id && s.Status == StoreStatus.Approved)) return HttpNotFound();

            comment = (comment ?? "").Trim();
            if (comment.Length > 300) comment = comment.Substring(0, 300);
            db.Reviews.Add(new Review { UserId = uid, StoreId = id, Rating = rating, Comment = comment, CreatedAt = DateTime.Now });
            db.SaveChanges();
            Flash("謝謝你的評論！");
            return RedirectToAction("Details", new { id });
        }
    }
}
