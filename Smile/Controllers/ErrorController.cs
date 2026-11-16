using System.Web.Mvc;

namespace Smile.Controllers
{
    /// <summary>友善的錯誤頁（不碰資料庫，資料庫出問題時也能顯示）</summary>
    public class ErrorController : Controller
    {
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        public ActionResult Oops()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            return View("Error");
        }
    }
}
