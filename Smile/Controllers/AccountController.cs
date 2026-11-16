using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    public class AccountController : BaseController
    {
        public ActionResult Register(string @ref, bool merchant = false)
        {
            ViewBag.Schools = new SelectList(db.Schools.ToList(), "Id", "ShortName");
            LoadRegisterExtras(@ref);
            return View(new RegisterViewModel { Ref = @ref, IsMerchant = merchant });
        }

        /// <summary>註冊頁的額外資料：邀請人的名字、目前最多人領的三張券（讓人知道註冊可以拿到什麼）</summary>
        private void LoadRegisterExtras(string inviteCode)
        {
            if (!string.IsNullOrWhiteSpace(inviteCode))
                ViewBag.InviterName = db.Users.Where(u => u.InviteCode == inviteCode).Select(u => u.DisplayName).FirstOrDefault();
            var today = DateTime.Today;
            ViewBag.Teasers = db.Coupons.Include("Store")
                .Where(c => c.IsActive && c.Store.Status == StoreStatus.Approved && c.StartDate <= today && c.EndDate >= today && c.IssuedCount < c.TotalQuantity)
                .OrderByDescending(c => c.IssuedCount).Take(3).ToList();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            var email = (model.Email ?? "").Trim().ToLowerInvariant();
            if (ModelState.IsValid && db.Users.Any(u => u.Email == email))
                ModelState.AddModelError("Email", "這個電子郵件已經註冊過了");

            if (!ModelState.IsValid)
            {
                ViewBag.Schools = new SelectList(db.Schools.ToList(), "Id", "ShortName", model.SchoolId);
                LoadRegisterExtras(model.Ref);
                return View(model);
            }

            string hash, salt;
            Security.HashPassword(model.Password, out hash, out salt);
            var user = new Models.User
            {
                Email = email,
                DisplayName = model.DisplayName.Trim(),
                Role = model.IsMerchant ? AppRoles.Merchant : AppRoles.Member,
                SchoolId = model.SchoolId,
                PasswordHash = hash,
                PasswordSalt = salt,
                MemberCode = NewUniqueMemberCode(),
                InviteCode = NewUniqueInviteCode(),
                CreatedAt = DateTime.Now
            };
            db.Users.Add(user);
            db.SaveChanges();

            // 從好友的邀請連結來的：記下邀請人，雙方直接成為好友
            if (!string.IsNullOrWhiteSpace(model.Ref))
            {
                var inviter = db.Users.FirstOrDefault(u => u.InviteCode == model.Ref);
                if (inviter != null && inviter.Id != user.Id)
                {
                    user.ReferredById = inviter.Id;
                    db.Friendships.Add(new Friendship { UserId = user.Id, FriendId = inviter.Id, CreatedAt = user.CreatedAt });
                    db.Friendships.Add(new Friendship { UserId = inviter.Id, FriendId = user.Id, CreatedAt = user.CreatedAt });
                    db.SaveChanges();
                }
            }

            Response.Cookies.Add(new HttpCookie("welcomed", "1") { Expires = DateTime.Now.AddYears(1), HttpOnly = true });
            SignIn(Response, user.Id, user.Email, user.Role, false);
            TempData["StartTour"] = true;
            return user.Role == AppRoles.Merchant ? RedirectToAction("CreateStore", "Merchant") : RedirectToAction("Index", "Home");
        }

        public ActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null || !Security.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError("", "電子郵件或密碼錯誤");
                return View(model);
            }

            Response.Cookies.Add(new HttpCookie("welcomed", "1") { Expires = DateTime.Now.AddYears(1), HttpOnly = true });
            SignIn(Response, user.Id, user.Email, user.Role, model.RememberMe);
            if (user.PreferLargeText)
                Response.Cookies.Add(new HttpCookie("textsize", "large") { Expires = DateTime.Now.AddYears(1), HttpOnly = true });

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
            if (user.Role == AppRoles.Admin) return RedirectToAction("Stores", "Admin");
            if (user.Role == AppRoles.Merchant) return RedirectToAction("Index", "Merchant");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Flash("已登出");
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult Settings()
        {
            var user = db.Users.Find(CurrentUserId);
            if (user == null) return RedirectToAction("Login");
            ViewBag.Schools = new SelectList(db.Schools.ToList(), "Id", "ShortName", user.SchoolId);
            return View(new ProfileViewModel
            {
                Email = user.Email,
                RoleName = AppRoles.Display(user.Role),
                DisplayName = user.DisplayName,
                SchoolId = user.SchoolId,
                PreferLargeText = user.PreferLargeText,
                CarrierCode = user.CarrierCode
            });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Settings(ProfileViewModel model)
        {
            var user = db.Users.Find(CurrentUserId);
            if (user == null) return RedirectToAction("Login");

            var changingPassword = !string.IsNullOrEmpty(model.NewPassword);
            if (changingPassword && !Security.VerifyPassword(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                ModelState.AddModelError("CurrentPassword", "目前密碼不正確");

            var carrier = (model.CarrierCode ?? "").Trim().ToUpperInvariant();
            if (carrier.Length > 0 && !carrier.StartsWith("/")) carrier = "/" + carrier;
            if (carrier.Length > 0 && !Security.IsValidCarrier(carrier))
                ModelState.AddModelError("CarrierCode", "手機條碼格式是斜線加 7 碼，例如 /ABC1234");

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                model.RoleName = AppRoles.Display(user.Role);
                ViewBag.Schools = new SelectList(db.Schools.ToList(), "Id", "ShortName", model.SchoolId);
                return View(model);
            }

            user.DisplayName = model.DisplayName.Trim();
            user.SchoolId = model.SchoolId;
            user.PreferLargeText = model.PreferLargeText;
            user.CarrierCode = carrier.Length == 0 ? null : carrier;
            if (changingPassword)
            {
                string hash, salt;
                Security.HashPassword(model.NewPassword, out hash, out salt);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
            }
            db.SaveChanges();
            Response.Cookies.Add(new HttpCookie("textsize", user.PreferLargeText ? "large" : "normal") { Expires = DateTime.Now.AddYears(1), HttpOnly = true });
            Flash(changingPassword ? "資料與密碼已更新" : "資料已更新");
            return RedirectToAction("Settings");
        }
    }
}
