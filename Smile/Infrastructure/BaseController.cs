using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Smile.DAL;

namespace Smile.Infrastructure
{
    /// <summary>所有 Controller 共用：資料庫、目前使用者、訊息提示</summary>
    public abstract class BaseController : Controller
    {
        protected readonly SmileContext db = new SmileContext();

        protected int? CurrentUserId
        {
            get
            {
                var id = User != null ? User.Identity as FormsIdentity : null;
                if (id == null || !id.IsAuthenticated) return null;
                int uid;
                var parts = (id.Ticket.UserData ?? "").Split('|');
                return parts.Length > 0 && int.TryParse(parts[0], out uid) ? uid : (int?)null;
            }
        }

        /// <summary>產生資料庫中尚未使用的會員編號</summary>
        protected string NewUniqueMemberCode()
        {
            string code;
            do { code = Security.NewMemberCode(); } while (db.Users.Any(u => u.MemberCode == code));
            return code;
        }

        /// <summary>產生資料庫中尚未使用的邀請碼</summary>
        protected string NewUniqueInviteCode()
        {
            string code;
            do { code = Security.NewInviteCode(); } while (db.Users.Any(u => u.InviteCode == code));
            return code;
        }

        /// <summary>
        /// 檢查登入的人有沒有新達成的徽章：有的話寫進資料庫，並放進 ViewBag.NewBadges 讓版面跳出慶祝。
        /// 回傳目前的累積數字（沒登入回傳 null）。
        /// </summary>
        protected UserStats SyncBadges()
        {
            var uid = CurrentUserId;
            if (uid == null) return null;
            var stats = Progress.GetStats(db, uid.Value);
            var fresh = Progress.Sync(db, uid.Value, stats);
            if (fresh.Count > 0) ViewBag.NewBadges = fresh;
            return stats;
        }

        protected void Flash(string message, string type = "success")
        {
            TempData["FlashMessage"] = message;
            TempData["FlashType"] = type;
        }

        public static void SignIn(HttpResponseBase response, int userId, string email, string role, bool persistent)
        {
            var ticket = new FormsAuthenticationTicket(1, email, DateTime.Now, DateTime.Now.AddHours(8), persistent, userId + "|" + role);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            };
            if (persistent) cookie.Expires = ticket.Expiration;
            response.Cookies.Add(cookie);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
