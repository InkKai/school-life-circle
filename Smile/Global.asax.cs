using System;
using System.Data.Entity;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using Smile.DAL;

namespace Smile
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(new SmileInitializer());
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // 從表單驗證票證取出角色，讓 [Authorize(Roles = "...")] 可以運作
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var ctx = HttpContext.Current;
            var identity = ctx.User != null ? ctx.User.Identity as FormsIdentity : null;
            if (identity == null || !identity.IsAuthenticated) return;
            var parts = (identity.Ticket.UserData ?? "").Split('|');
            var role = parts.Length > 1 ? parts[1] : "";
            ctx.User = new GenericPrincipal(identity, new[] { role });
        }
    }
}
