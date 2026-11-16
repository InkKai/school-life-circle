using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;

namespace Smile.Controllers
{
    [Authorize]
    public class FavoritesController : BaseController
    {
        public ActionResult Index()
        {
            var uid = CurrentUserId.Value;
            SyncBadges();
            var favs = db.Favorites.Include("Store").Include("Store.Category")
                .Where(f => f.UserId == uid).OrderByDescending(f => f.CreatedAt).ToList();
            return View(favs);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            var uid = CurrentUserId.Value;
            var fav = db.Favorites.FirstOrDefault(f => f.Id == id && f.UserId == uid);
            if (fav != null) { db.Favorites.Remove(fav); db.SaveChanges(); Flash("已移除收藏"); }
            return RedirectToAction("Index");
        }
    }
}
