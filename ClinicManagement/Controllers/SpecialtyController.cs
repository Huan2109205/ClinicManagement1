
using System.Linq;
using System.Net;
using System.Data.Entity;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class SpecialtyController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // GET: Specialty
        public ActionResult Index(string keyword)
        {
            var specialties = db.Specialties.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                specialties = specialties.Where(s =>
                    s.SpecialtyCode.Contains(keyword) ||
                    s.SpecialtyName.Contains(keyword));
            }

            return View(
                specialties.OrderBy(s => s.SpecialtyID).ToList()
            );
        }

        // GET: Specialty/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Specialty specialty = db.Specialties.Find(id);

            if (specialty == null)
                return HttpNotFound();

            ViewBag.DoctorCount = db.Doctors.Count(
                d => d.SpecialtyID == id
            );

            return View(specialty);
        }

        // GET: Specialty/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Specialty/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Specialty specialty)
        {
            if (specialty.SpecialtyCode != null)
                specialty.SpecialtyCode = specialty.SpecialtyCode.Trim();

            if (specialty.SpecialtyName != null)
                specialty.SpecialtyName = specialty.SpecialtyName.Trim();

            if (!string.IsNullOrWhiteSpace(specialty.SpecialtyCode))
            {
                bool codeExists = db.Specialties.Any(s =>
                    s.SpecialtyCode == specialty.SpecialtyCode);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        "SpecialtyCode",
                        "Mã chuyên khoa đã tồn tại."
                    );
                }
            }

            if (ModelState.IsValid)
            {
                db.Specialties.Add(specialty);
                db.SaveChanges();

                TempData["Success"] = "Thêm chuyên khoa thành công.";
                return RedirectToAction("Index");
            }

            return View(specialty);
        }

        // GET: Specialty/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Specialty specialty = db.Specialties.Find(id);

            if (specialty == null)
                return HttpNotFound();

            return View(specialty);
        }

        // POST: Specialty/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Specialty specialty)
        {
            var existing = db.Specialties.Find(specialty.SpecialtyID);

            if (existing == null)
                return HttpNotFound();

            if (specialty.SpecialtyCode != null)
                specialty.SpecialtyCode = specialty.SpecialtyCode.Trim();

            if (specialty.SpecialtyName != null)
                specialty.SpecialtyName = specialty.SpecialtyName.Trim();

            if (!string.IsNullOrWhiteSpace(specialty.SpecialtyCode))
            {
                bool codeExists = db.Specialties.Any(s =>
                    s.SpecialtyCode == specialty.SpecialtyCode &&
                    s.SpecialtyID != specialty.SpecialtyID);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        "SpecialtyCode",
                        "Mã chuyên khoa đã được sử dụng."
                    );
                }
            }

            if (ModelState.IsValid)
            {
                existing.SpecialtyCode = specialty.SpecialtyCode;
                existing.SpecialtyName = specialty.SpecialtyName;
                existing.Description = specialty.Description;
                existing.IsActive = specialty.IsActive;

                db.SaveChanges();

                TempData["Success"] = "Cập nhật chuyên khoa thành công.";
                return RedirectToAction("Index");
            }

            return View(specialty);
        }

        // GET: Specialty/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Specialty specialty = db.Specialties.Find(id);

            if (specialty == null)
                return HttpNotFound();

            int doctorCount = db.Doctors.Count(
                d => d.SpecialtyID == id
            );

            ViewBag.DoctorCount = doctorCount;

            return View(specialty);
        }

        // POST: Specialty/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Specialty specialty = db.Specialties.Find(id);

            if (specialty == null)
                return HttpNotFound();

            bool isUsed = db.Doctors.Any(
                d => d.SpecialtyID == id
            );

            if (isUsed)
            {
                TempData["Error"] =
                    "Không thể xóa chuyên khoa đang được bác sĩ sử dụng. " +
                    "Bạn hãy chuyển chuyên khoa sang trạng thái ngừng hoạt động.";

                return RedirectToAction("Index");
            }

            db.Specialties.Remove(specialty);
            db.SaveChanges();

            TempData["Success"] = "Xóa chuyên khoa thành công.";

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
