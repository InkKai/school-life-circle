using System;
using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;

namespace Smile.Controllers
{
    [Authorize]
    public class MyCouponsController : BaseController
    {
        // 我的優惠券：可使用／已使用／已過期
        public ActionResult Index(string status)
        {
            var uid = CurrentUserId.Value;
            var today = DateTime.Today;
            SyncBadges();
            var all = db.UserCoupons.Include("Coupon").Include("Coupon.Store").Where(u => u.UserId == uid).OrderByDescending(u => u.ClaimedAt).ToList();
            ViewBag.Status = string.IsNullOrEmpty(status) ? "available" : status;
            switch ((string)ViewBag.Status)
            {
                case "used": return View(all.Where(u => u.Status == CouponStatus.Used).ToList());
                case "expired": return View(all.Where(u => u.Status == CouponStatus.Available && u.Coupon.EndDate < today).ToList());
                default: return View(all.Where(u => u.Status == CouponStatus.Available && u.Coupon.EndDate >= today).ToList());
            }
        }

        // 優惠券 QR Code
        public ActionResult Show(int id)
        {
            var uid = CurrentUserId.Value;
            var uc = db.UserCoupons.Include("Coupon").Include("Coupon.Store").FirstOrDefault(u => u.Id == id && u.UserId == uid);
            if (uc == null) return HttpNotFound();
            SyncBadges();
            ViewBag.ShareUrl = uc.ShareToken == null ? null : Url.RouteUrl("SharedCoupon", new { token = uc.ShareToken }, Request.Url.Scheme);
            return View(uc);
        }

        // 產生分享連結，讓家人不必註冊也能出示
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Share(int id)
        {
            var uid = CurrentUserId.Value;
            var uc = db.UserCoupons.FirstOrDefault(u => u.Id == id && u.UserId == uid);
            if (uc == null) return HttpNotFound();
            if (uc.Status != CouponStatus.Available) { Flash("這張優惠券已使用，不能分享", "warning"); return RedirectToAction("Show", new { id }); }
            if (uc.ShareToken == null) { uc.ShareToken = Security.NewShareToken(); db.SaveChanges(); }
            Flash("分享連結已產生，可以用 LINE 傳給家人。");
            return RedirectToAction("Show", new { id });
        }

        // 收到分享連結的人看到的頁面（不需登入）
        [AllowAnonymous]
        public ActionResult Shared(string token)
        {
            if (string.IsNullOrEmpty(token)) return HttpNotFound();
            var uc = db.UserCoupons.Include("Coupon").Include("Coupon.Store").FirstOrDefault(u => u.ShareToken == token);
            if (uc == null) return HttpNotFound();
            return View(uc);
        }
    }
}
