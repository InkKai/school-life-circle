using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;

namespace Smile.Controllers
{
    /// <summary>活動：配合學校行事曆的主題優惠檔期</summary>
    public class CampaignsController : BaseController
    {
        // 活動列表：進行中與即將開始
        public ActionResult Index()
        {
            var today = DateTime.Today;
            ViewBag.Counts = db.Coupons
                .Where(c => c.CampaignId != null && c.IsActive && c.Store.Status == StoreStatus.Approved && c.EndDate >= today)
                .GroupBy(c => c.CampaignId.Value).ToDictionary(g => g.Key, g => g.Count());
            return View(db.Campaigns.Where(c => c.IsActive && c.EndDate >= today).OrderBy(c => c.StartDate).ToList());
        }

        // 活動頁：這一檔有哪些優惠
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var campaign = db.Campaigns.Find(id.Value);
            if (campaign == null || (!campaign.IsActive && !User.IsInRole(AppRoles.Admin))) return HttpNotFound();

            var today = DateTime.Today;
            ViewBag.Coupons = db.Coupons.Include("Store").Include("Store.Category")
                .Where(c => c.CampaignId == campaign.Id && c.IsActive && c.Store.Status == StoreStatus.Approved && c.StartDate <= today && c.EndDate >= today)
                .OrderBy(c => c.EndDate).ToList();
            var uid = CurrentUserId;
            ViewBag.Claimed = uid == null ? new int[0] : db.UserCoupons.Where(u => u.UserId == uid).Select(u => u.CouponId).ToArray();
            return View(campaign);
        }
    }
}
