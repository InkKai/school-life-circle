using System;
using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;

namespace Smile.Controllers
{
    /// <summary>好友：用會員編號加好友、把用不到的券送給好友</summary>
    [Authorize]
    public class FriendsController : BaseController
    {
        public ActionResult Index()
        {
            var uid = CurrentUserId.Value;
            var me = db.Users.Find(uid);
            ViewBag.Me = me;
            ViewBag.InviteUrl = Url.Action("Register", "Account", new { @ref = me.InviteCode }, Request.Url.Scheme);
            ViewBag.ReferralCount = db.Users.Count(u => u.ReferredById == uid);
            SyncBadges();
            ViewBag.Received = db.UserCoupons.Include("Coupon.Store").Include("GiftedFrom")
                .Where(u => u.UserId == uid && u.GiftedFromId != null).OrderByDescending(u => u.ClaimedAt).Take(5).ToList();
            return View(db.Friendships.Include("Friend").Where(f => f.UserId == uid).OrderBy(f => f.Friend.DisplayName).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Add(string code)
        {
            var uid = CurrentUserId.Value;
            code = new string((code ?? "").Where(char.IsDigit).ToArray());
            var friend = code.Length == 10 ? db.Users.FirstOrDefault(u => u.MemberCode == code) : null;
            if (friend == null) { Flash("找不到這個會員編號", "warning"); return RedirectToAction("Index"); }
            if (friend.Id == uid) { Flash("這是你自己的編號喔", "info"); return RedirectToAction("Index"); }
            if (db.Friendships.Any(f => f.UserId == uid && f.FriendId == friend.Id)) { Flash(friend.DisplayName + " 已經是你的好友了", "info"); return RedirectToAction("Index"); }

            var now = DateTime.Now;
            db.Friendships.Add(new Friendship { UserId = uid, FriendId = friend.Id, CreatedAt = now });
            if (!db.Friendships.Any(f => f.UserId == friend.Id && f.FriendId == uid))
                db.Friendships.Add(new Friendship { UserId = friend.Id, FriendId = uid, CreatedAt = now });
            db.SaveChanges();
            Flash("已加 " + friend.DisplayName + " 為好友");
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            var uid = CurrentUserId.Value;
            var pair = db.Friendships.Where(f => (f.UserId == uid && f.FriendId == id) || (f.UserId == id && f.FriendId == uid)).ToList();
            if (!pair.Any()) return HttpNotFound();
            db.Friendships.RemoveRange(pair);
            db.SaveChanges();
            Flash("已刪除好友");
            return RedirectToAction("Index");
        }

        // 送券給好友：把一張還沒用的券轉到好友名下
        public ActionResult Gift(int id)
        {
            var uid = CurrentUserId.Value;
            var uc = db.UserCoupons.Include("Coupon.Store").FirstOrDefault(u => u.Id == id && u.UserId == uid);
            if (uc == null) return HttpNotFound();
            ViewBag.Friends = db.Friendships.Include("Friend").Where(f => f.UserId == uid).Select(f => f.Friend).OrderBy(f => f.DisplayName).ToList();
            return View(uc);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Gift(int id, int friendId)
        {
            var uid = CurrentUserId.Value;
            var uc = db.UserCoupons.Include("Coupon").FirstOrDefault(u => u.Id == id && u.UserId == uid);
            if (uc == null) return HttpNotFound();
            if (!db.Friendships.Any(f => f.UserId == uid && f.FriendId == friendId)) return HttpNotFound();
            if (uc.Status != CouponStatus.Available || uc.Coupon.EndDate < DateTime.Today)
            {
                Flash("這張券已經用過或過期，不能送", "warning");
                return RedirectToAction("Index", "MyCoupons");
            }
            // 每人每款限一張：好友已經有同一款就不能送
            if (db.UserCoupons.Any(u => u.UserId == friendId && u.CouponId == uc.CouponId))
            {
                Flash("對方已經有這張優惠了", "info");
                return RedirectToAction("Gift", new { id });
            }
            var friend = db.Users.Find(friendId);
            uc.UserId = friendId;
            uc.GiftedFromId = uid;
            uc.ShareToken = null;   // 舊的分享連結失效
            db.SaveChanges();
            Flash("已把「" + uc.Coupon.Title + "」送給 " + friend.DisplayName);
            return RedirectToAction("Index", "MyCoupons");
        }
    }
}
