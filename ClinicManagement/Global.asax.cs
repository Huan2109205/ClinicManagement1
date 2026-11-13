using ClinicManagement.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace ClinicManagement
{
    public class MvcApplication : System.Web.HttpApplication
    {

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }


        protected void Application_AuthenticateRequest()
        {
            HttpCookie authCookie =
                Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie == null || string.IsNullOrEmpty(authCookie.Value))
                return;

            try
            {
                FormsAuthenticationTicket ticket =
                    FormsAuthentication.Decrypt(authCookie.Value);

                if (ticket == null || ticket.Expired)
                {
                    FormsAuthentication.SignOut();
                    return;
                }

                string username = ticket.Name;

                if (string.IsNullOrWhiteSpace(username))
                {
                    FormsAuthentication.SignOut();
                    return;
                }

                using (var db = new ClinicDbContext())
                {
                    // Kiểm tra tài khoản trong database
                    var user = db.Users.FirstOrDefault(
                        u => u.Username == username
                    );

                    // Không tồn tại hoặc đã bị khóa
                    if (user == null || !user.IsActive)
                    {
                        FormsAuthentication.SignOut();
                        return;
                    }

                    // Lấy quyền mới nhất từ database
                    var role = db.Roles.Find(user.RoleID);

                    if (role == null)
                    {
                        FormsAuthentication.SignOut();
                        return;
                    }

                    GenericIdentity identity =
                        new GenericIdentity(user.Username, "Forms");

                    GenericPrincipal principal =
                        new GenericPrincipal(
                            identity,
                            new string[] { role.RoleName }
                        );

                    HttpContext.Current.User = principal;
                    System.Threading.Thread.CurrentPrincipal = principal;
                }
            }
            catch
            {
                FormsAuthentication.SignOut();
            }
        }

    }
}
