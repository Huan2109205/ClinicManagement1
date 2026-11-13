
using System;
using System.Linq;
using System.Net;
using System.Data.Entity;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist,Doctor")]
    public class MedicineController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // DANH SÁCH THUỐC
        public ActionResult Index(string keyword)
        {
            var medicines = db.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                medicines = medicines.Where(m =>
                    m.MedicineCode.Contains(keyword) ||
                    m.MedicineName.Contains(keyword) ||
                    m.ActiveIngredient.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(medicines.OrderBy(m => m.MedicineName).ToList());
        }

        // THÊM THUỐC
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult Create(Medicine medicine)
        {
            if (db.Medicines.Any(m =>
                m.MedicineCode == medicine.MedicineCode))
            {
                ModelState.AddModelError("MedicineCode",
                    "Mã thuốc đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                medicine.IsActive = true;

                db.Medicines.Add(medicine);
                db.SaveChanges();

                TempData["Success"] = "Thêm thuốc thành công.";

                return RedirectToAction("Index");
            }

            return View(medicine);
        }

        // CHỈNH SỬA THUỐC
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var medicine = db.Medicines.Find(id);

            if (medicine == null)
                return HttpNotFound();

            return View(medicine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult Edit(Medicine medicine)
        {
            var existing = db.Medicines.Find(medicine.MedicineID);

            if (existing == null)
                return HttpNotFound();

            if (db.Medicines.Any(m =>
                m.MedicineCode == medicine.MedicineCode &&
                m.MedicineID != medicine.MedicineID))
            {
                ModelState.AddModelError("MedicineCode",
                    "Mã thuốc đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                existing.MedicineCode = medicine.MedicineCode;
                existing.MedicineName = medicine.MedicineName;
                existing.ActiveIngredient = medicine.ActiveIngredient;
                existing.Unit = medicine.Unit;
                existing.UnitPrice = medicine.UnitPrice;
                existing.Description = medicine.Description;

                db.SaveChanges();

                TempData["Success"] = "Cập nhật thuốc thành công.";

                return RedirectToAction("Index");
            }

            return View(medicine);
        }

        // NGỪNG SỬ DỤNG THUỐC (KHÔNG XÓA DỮ LIỆU)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult ToggleStatus(int id)
        {
            var medicine = db.Medicines.Find(id);

            if (medicine == null)
                return HttpNotFound();

            medicine.IsActive = !medicine.IsActive;

            db.SaveChanges();

            TempData["Success"] = medicine.IsActive
                ? "Đã kích hoạt thuốc."
                : "Đã ngừng sử dụng thuốc.";

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
