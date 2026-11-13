
using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // DANH SÁCH NHẬT KÝ
        public ActionResult Index(string keyword, string module, DateTime? fromDate, DateTime? toDate)
        {
            var logs = db.ActivityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                logs = logs.Where(x =>
                    x.UserName.Contains(keyword) ||
                    x.Action.Contains(keyword) ||
                    x.Description.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                logs = logs.Where(x => x.Module == module);
            }

            if (fromDate.HasValue)
            {
                logs = logs.Where(x => x.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                DateTime endDate = toDate.Value.Date.AddDays(1);
                logs = logs.Where(x => x.CreatedAt < endDate);
            }

            ViewBag.Keyword = keyword;
            ViewBag.Module = module;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            ViewBag.Modules = new SelectList(
                db.ActivityLogs
                    .Select(x => x.Module)
                    .Distinct()
                    .ToList()
                    .Select(x => new SelectListItem
                    {
                        Text = x,
                        Value = x
                    }),
                "Value",
                "Text",
                module
            );

            return View(logs
                .OrderByDescending(x => x.CreatedAt)
                .ToList());
        }

        // CHI TIẾT NHẬT KÝ
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(400);

            var log = db.ActivityLogs.Find(id);

            if (log == null)
                return HttpNotFound();

            return View(log);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
