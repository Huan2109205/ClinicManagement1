
using ClinicManagement.DAL;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ClinicManagement.Controllers
{
    public class DbTestController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                using (var db = new ClinicDbContext())
                {
                    int total = db.Patients.Count();

                    return Content(
                        "KET NOI SQL SERVER THANH CONG! " +
                        "So benh nhan hien co: " + total
                    );
                }
            }
            catch (Exception)
            {
                return Content(
                    "KET NOI THAT BAI! " +
                    "Hay kiem tra SQL Server va Web.config."
                );
            }
        }
    }
}
