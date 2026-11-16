using System;
using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    /// <summary>平台管理：店家審核與精選、活動管理、使用者管理、營運報表</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : BaseController
    {
        public ActionResult Stores(string status, string q)
        {
            status = string.IsNullOrEmpty(status) ? StoreStatus.Pending : status;
            ViewBag.Status = status;
            ViewBag.Query = q;
            ViewBag.PendingCount = db.Stores.Count(s => s.Status == StoreStatus.Pending);

            var query = db.Stores.Include("Category").Include("Owner").Where(s => s.Status == status);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(s => s.Name.Contains(kw));
            }
            else if (status == StoreStatus.Approved)
            {
                // 「已上架」預設只列已加入平台的店（示範＋店家申請）；OpenStreetMap 名錄有一千多間，要用搜尋找
                query = query.Where(s => s.DataSource != DataSources.Osm);
            }
            ViewBag.Total = query.Count();
            return View(query.OrderByDescending(s => s.IsFeatured).ThenByDescending(s => s.CreatedAt).Take(50).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Review(int id, string decision, bool verified = false)
        {
            var store = db.Stores.Find(id);
            if (store == null) return HttpNotFound();
            store.Status = decision == "approve" ? StoreStatus.Approved : StoreStatus.Rejected;
            store.AccessibilityVerified = verified;
            db.SaveChanges();
            Flash("「" + store.Name + "」" + StoreStatus.Display(store.Status));
            return RedirectToAction("Stores");
        }

        // 精選店家：首頁的曝光位置（未來加值方案的內容），只有已加入平台的店能設定
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleFeatured(int id)
        {
            var store = db.Stores.Find(id);
            if (store == null || store.Status != StoreStatus.Approved) return HttpNotFound();
            if (store.DataSource == DataSources.Osm)
            {
                Flash("還沒加入平台的店家不能設為精選", "warning");
                return RedirectToAction("Stores", new { status = StoreStatus.Approved });
            }
            store.IsFeatured = !store.IsFeatured;
            db.SaveChanges();
            Flash("「" + store.Name + "」" + (store.IsFeatured ? "已設為精選，會出現在首頁" : "已取消精選"));
            return RedirectToAction("Stores", new { status = StoreStatus.Approved });
        }

        // ===== 活動管理 =====
        public ActionResult Campaigns()
        {
            ViewBag.Counts = db.Coupons.Where(c => c.CampaignId != null).GroupBy(c => c.CampaignId.Value).ToDictionary(g => g.Key, g => g.Count());
            return View(db.Campaigns.OrderByDescending(c => c.StartDate).ToList());
        }

        public ActionResult CampaignForm(int? id)
        {
            var campaign = id.HasValue
                ? db.Campaigns.Find(id.Value)
                : new Campaign { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(13), Theme = "indigo", IsActive = true };
            if (campaign == null) return HttpNotFound();
            return View(campaign);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveCampaign([Bind(Include = "Id,Title,Tagline,StartDate,EndDate,Theme,IsActive")] Campaign campaign)
        {
            if (campaign.EndDate < campaign.StartDate) ModelState.AddModelError("EndDate", "結束日期不能早於開始日期");
            if (!CampaignThemes.All.Any(t => t[0] == campaign.Theme)) ModelState.AddModelError("Theme", "請選擇主題色");
            if (!ModelState.IsValid) return View("CampaignForm", campaign);

            if (campaign.Id == 0)
            {
                db.Campaigns.Add(campaign);
            }
            else
            {
                var existing = db.Campaigns.Find(campaign.Id);
                if (existing == null) return HttpNotFound();
                existing.Title = campaign.Title;
                existing.Tagline = campaign.Tagline;
                existing.StartDate = campaign.StartDate;
                existing.EndDate = campaign.EndDate;
                existing.Theme = campaign.Theme;
                existing.IsActive = campaign.IsActive;
            }
            db.SaveChanges();
            Flash("已儲存活動「" + campaign.Title + "」");
            return RedirectToAction("Campaigns");
        }

        public ActionResult Users(string q)
        {
            var query = db.Users.Include("School").AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.Email.Contains(q) || u.DisplayName.Contains(q));
            ViewBag.Query = q;
            return View(query.OrderBy(u => u.Id).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangeRole(int id, string role)
        {
            if (role != AppRoles.Member && role != AppRoles.Merchant && role != AppRoles.Admin) return new HttpStatusCodeResult(400);
            if (id == CurrentUserId) { Flash("不能修改自己的角色", "warning"); return RedirectToAction("Users"); }
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();
            user.Role = role;
            db.SaveChanges();
            Flash(user.DisplayName + " 已改為" + AppRoles.Display(role) + "（下次登入生效）");
            return RedirectToAction("Users");
        }

        public ActionResult Reports()
        {
            var cats = db.StoreCategories.OrderBy(c => c.SortOrder).ToList();
            var rows = cats.Select(c => new CategoryReportRow
            {
                Category = c.Name,
                Stores = db.Stores.Count(s => s.CategoryId == c.Id && s.Status == StoreStatus.Approved),
                Claims = db.UserCoupons.Count(u => u.Coupon.Store.CategoryId == c.Id),
                Redemptions = db.Redemptions.Count(r => r.Store.CategoryId == c.Id)
            }).ToList();

            var weekAgo = DateTime.Now.AddDays(-7);
            return View(new ReportViewModel
            {
                Users = db.Users.Count(),
                Merchants = db.Users.Count(u => u.Role == AppRoles.Merchant),
                ApprovedStores = db.Stores.Count(s => s.Status == StoreStatus.Approved),
                PendingStores = db.Stores.Count(s => s.Status == StoreStatus.Pending),
                Claims = db.UserCoupons.Count(),
                Redemptions = db.Redemptions.Count(),
                ByCategory = rows,

                // 成長漏斗：註冊 → 領券（啟用）→ 到店使用 → 回訪；只算人數，不看個人紀錄
                Members = db.Users.Count(u => u.Role == AppRoles.Member),
                ClaimedMembers = db.Users.Count(u => u.Role == AppRoles.Member && db.UserCoupons.Any(c => c.UserId == u.Id || c.GiftedFromId == u.Id)),
                RedeemedMembers = db.Users.Count(u => u.Role == AppRoles.Member && db.UserCoupons.Any(c => c.UserId == u.Id && c.Status == CouponStatus.Used)),
                ReturnedMembers = db.Users.Count(u => u.Role == AppRoles.Member && db.UserCoupons.Count(c => c.UserId == u.Id && c.Status == CouponStatus.Used) >= 2),
                ReferredMembers = db.Users.Count(u => u.ReferredById != null),
                NewMembers7d = db.Users.Count(u => u.Role == AppRoles.Member && u.CreatedAt >= weekAgo),
                FriendPairs = db.Friendships.Count() / 2,
                StampsGiven = db.StampRecords.Where(r => r.Kind == StampKinds.Stamp).Select(r => (int?)r.Change).Sum() ?? 0,
                FeaturedStores = db.Stores.Count(s => s.IsFeatured),
                JoinedStores = db.Stores.Count(s => s.Status == StoreStatus.Approved && s.DataSource != DataSources.Osm)
            });
        }
    }
}
