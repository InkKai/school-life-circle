using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;

namespace Smile.Controllers
{
    public class CouponsController : BaseController
    {
        // 優惠列表：全部／快到期／依分類
        public ActionResult Index(string filter, int? categoryId)
        {
            var today = DateTime.Today;
            var query = db.Coupons.Include("Store").Include("Store.Category")
                .Where(c => c.IsActive && c.Store.Status == StoreStatus.Approved && c.StartDate <= today && c.EndDate >= today);
            if (filter == "expiring")
            {
                var soon = today.AddDays(7);
                query = query.Where(c => c.EndDate <= soon);
            }
            if (categoryId.HasValue) query = query.Where(c => c.Store.CategoryId == categoryId.Value);

            ViewBag.Filter = filter;
            ViewBag.Campaign = db.Campaigns.Where(c => c.IsActive && c.StartDate <= today && c.EndDate >= today).OrderBy(c => c.EndDate).FirstOrDefault();
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = db.StoreCategories.OrderBy(c => c.SortOrder).ToList();
            var uid = CurrentUserId;
            ViewBag.Claimed = uid == null ? new int[0] : db.UserCoupons.Where(u => u.UserId == uid).Select(u => u.CouponId).ToArray();
            var list = query.OrderBy(c => c.EndDate).ToList();
            if (filter == "nearby")
            {
                // 附近：依離選定學校（預設第一所）的距離排序
                var school = db.Schools.OrderBy(x => x.Id).FirstOrDefault();
                if (school != null)
                    list = list.OrderBy(c => Geo.DistanceMeters(school.Latitude, school.Longitude, c.Store.Latitude, c.Store.Longitude)).ToList();
            }
            return View(list);
        }

        // 優惠詳情
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var coupon = db.Coupons.Include("Store").Include("Store.Category").FirstOrDefault(c => c.Id == id);
            if (coupon == null || coupon.Store.Status != StoreStatus.Approved) return HttpNotFound();
            var uid = CurrentUserId;
            ViewBag.Claimed = uid != null && db.UserCoupons.Any(u => u.UserId == uid && u.CouponId == coupon.Id);
            return View(coupon);
        }

        // 領取優惠券：每人每款限領一張
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Claim(int id)
        {
            var uid = CurrentUserId.Value;
            var coupon = db.Coupons.Include("Store").FirstOrDefault(c => c.Id == id);
            if (coupon == null || coupon.Store.Status != StoreStatus.Approved) return HttpNotFound();

            if (db.UserCoupons.Any(u => u.UserId == uid && u.CouponId == id))
            {
                Flash("你已經領過這張優惠券了", "info");
                return RedirectToAction("Index", "MyCoupons");
            }
            if (!coupon.IsClaimable(DateTime.Today))
            {
                Flash("這張優惠券已領完或不在活動期間", "warning");
                return RedirectToAction("Details", new { id });
            }

            string code;
            do { code = Security.NewCouponCode(); } while (db.UserCoupons.Any(u => u.Code == code));

            var uc = new UserCoupon { CouponId = id, UserId = uid, Code = code, Status = CouponStatus.Available, ClaimedAt = DateTime.Now };
            db.UserCoupons.Add(uc);
            coupon.IssuedCount += 1;
            db.SaveChanges();
            Flash("領取成功！到店出示 QR Code 就能使用。");
            TempData["Celebrate"] = true;
            return RedirectToAction("Show", "MyCoupons", new { id = uc.Id });
        }
    }
}
