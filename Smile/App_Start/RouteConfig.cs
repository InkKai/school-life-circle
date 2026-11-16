using System.Web.Mvc;
using System.Web.Routing;

namespace Smile
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // 分享給家人的優惠券短網址：/s/{token}
            routes.MapRoute(
                name: "SharedCoupon",
                url: "s/{token}",
                defaults: new { controller = "MyCoupons", action = "Shared" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
