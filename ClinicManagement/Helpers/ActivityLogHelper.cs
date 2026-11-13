
using System;
using System.Web;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Helpers
{
    public static class ActivityLogHelper
    {
        public static void Write(
            string action,
            string module,
            string description)
        {
            try
            {
                var context = HttpContext.Current;

                string userName = "Không xác định";
                string ipAddress = "";

                if (context != null)
                {
                    if (context.User != null &&
                        context.User.Identity != null &&
                        context.User.Identity.IsAuthenticated)
                    {
                        userName = context.User.Identity.Name;
                    }

                    ipAddress = context.Request.UserHostAddress;
                }

                using (var db = new ClinicDbContext())
                {
                    var log = new ActivityLog
                    {
                        UserName = userName,
                        Action = action,
                        Module = module,
                        Description = description,
                        CreatedAt = DateTime.Now,
                        IPAddress = ipAddress
                    };

                    db.ActivityLogs.Add(log);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Không làm gián đoạn chức năng chính
                // nếu việc ghi nhật ký gặp lỗi.
            }
        }
    }
}
