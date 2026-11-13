using ClinicManagement.DAL;
using ClinicManagement.Helpers;
using ClinicManagement.Models;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public class PatientController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // GET: Patient
        // Danh sach va tim kiem benh nhan
        public ActionResult Index(string keyword)
        {
            var patients = db.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                patients = patients.Where(p =>
                    p.PatientCode.Contains(keyword) ||
                    p.FullName.Contains(keyword) ||
                    p.Phone.Contains(keyword) ||
                    p.Email.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(patients
                .OrderBy(p => p.PatientID)
                .ToList());
        }

        // GET: Patient/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Patient patient = db.Patients.Find(id);

            if (patient == null)
                return HttpNotFound();

            return View(patient);
        }

        // GET: Patient/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Patient patient)
        {
            if (patient.PatientCode != null)
                patient.PatientCode = patient.PatientCode.Trim();

            if (patient.FullName != null)
                patient.FullName = patient.FullName.Trim();

            if (patient.Phone != null)
                patient.Phone = patient.Phone.Trim();

            if (patient.Email != null)
                patient.Email = patient.Email.Trim();

            if (patient.Address != null)
                patient.Address = patient.Address.Trim();

            bool codeExists = !string.IsNullOrWhiteSpace(patient.PatientCode)
                && db.Patients.Any(p =>
                    p.PatientCode == patient.PatientCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "PatientCode",
                    "Mã bệnh nhân đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                db.Patients.Add(patient);
                db.SaveChanges();

                ActivityLogHelper.Write(
                    "Thêm mới",
                    "Bệnh nhân",
                    "Thêm bệnh nhân: " + patient.FullName
                );
                TempData["Success"] = "Thêm bệnh nhân thành công.";

                return RedirectToAction("Index");
            }

            return View(patient);
        }

        // GET: Patient/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Patient patient = db.Patients.Find(id);

            if (patient == null)
                return HttpNotFound();

            return View(patient);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Patient patient)
        {
            Patient existingPatient = db.Patients.Find(patient.PatientID);

            if (existingPatient == null)
                return HttpNotFound();

            if (patient.PatientCode != null)
                patient.PatientCode = patient.PatientCode.Trim();

            if (patient.FullName != null)
                patient.FullName = patient.FullName.Trim();

            if (patient.Phone != null)
                patient.Phone = patient.Phone.Trim();

            if (patient.Email != null)
                patient.Email = patient.Email.Trim();

            if (patient.Address != null)
                patient.Address = patient.Address.Trim();

            bool codeExists = !string.IsNullOrWhiteSpace(patient.PatientCode)
                && db.Patients.Any(p =>
                    p.PatientCode == patient.PatientCode &&
                    p.PatientID != patient.PatientID);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "PatientCode",
                    "Mã bệnh nhân đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                // Chi cap nhat cac truong duoc phep
                existingPatient.PatientCode = patient.PatientCode;
                existingPatient.FullName = patient.FullName;
                existingPatient.DateOfBirth = patient.DateOfBirth;
                existingPatient.Gender = patient.Gender;
                existingPatient.Phone = patient.Phone;
                existingPatient.Address = patient.Address;
                existingPatient.Email = patient.Email;

                db.SaveChanges();
                ActivityLogHelper.Write(
    "Cập nhật",
    "Bệnh nhân",
    "Cập nhật thông tin bệnh nhân: " + patient.FullName
);

                TempData["Success"] = "Cập nhật bệnh nhân thành công.";

                return RedirectToAction("Index");
            }

            return View(patient);
        }

        // GET: Patient/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Patient patient = db.Patients.Find(id);

            if (patient == null)
                return HttpNotFound();

            return View(patient);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Patient patient = db.Patients.Find(id);

            if (patient == null)
                return HttpNotFound();

            db.Patients.Remove(patient);
            db.SaveChanges();
            ActivityLogHelper.Write(
    "Xóa",
    "Bệnh nhân",
    "Xóa bệnh nhân: " + patient.FullName
);


            TempData["Success"] = "Xóa bệnh nhân thành công.";

            return RedirectToAction("Index");
        }
       
        // Giu lai duong dan Search cu neu dang duoc su dung
        public ActionResult Search(string keyword)
        {
            return RedirectToAction("Index", new { keyword = keyword });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}