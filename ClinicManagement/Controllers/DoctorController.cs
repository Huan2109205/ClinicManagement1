
using System.Linq;
using System.Net;
using System.Data.Entity;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;
using ClinicManagement.Helpers;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public class DoctorController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // NAP DANH SACH CHUYEN KHOA
        private void LoadSpecialties(int? selectedId = null)
        {
            var specialties = db.Specialties
                .OrderBy(s => s.SpecialtyName)
                .ToList();

            ViewBag.SpecialtyID = new SelectList(
                specialties,
                "SpecialtyID",
                "SpecialtyName",
                selectedId
            );
        }

        // DANH SACH BAC SI
        public ActionResult Index(string keyword)
        {
            var doctors = db.Doctors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                doctors = doctors.Where(d =>
                    d.DoctorCode.Contains(keyword) ||
                    d.FullName.Contains(keyword) ||
                    d.Specialty.Contains(keyword) ||
                    d.SpecialtyNavigation.SpecialtyName.Contains(keyword) ||
                    d.Phone.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(doctors.ToList());
        }

        // GET: Doctor/Create
        public ActionResult Create()
        {
            LoadSpecialties();
            return View();
        }

        // POST: Doctor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Doctor doctor)
        {
            if (!doctor.SpecialtyID.HasValue)
            {
                ModelState.AddModelError(
                    "SpecialtyID",
                    "Vui lòng chọn chuyên khoa"
                );
            }
            else
            {
                var specialty = db.Specialties.Find(doctor.SpecialtyID);

                if (specialty == null)
                {
                    ModelState.AddModelError(
                        "SpecialtyID",
                        "Chuyên khoa không tồn tại"
                    );
                }
                else
                {
                    doctor.Specialty = specialty.SpecialtyName;
                }
            }

            if (ModelState.IsValid)
            {
                db.Doctors.Add(doctor);
                db.SaveChanges();

                ActivityLogHelper.Write(
                    "Thêm mới",
                    "Bác sĩ",
                    "Thêm bác sĩ: " + doctor.FullName
                );

                TempData["Success"] = "Thêm bác sĩ thành công.";

                return RedirectToAction("Index");
            }

            LoadSpecialties(doctor.SpecialtyID);
            return View(doctor);
        }

        // GET: Doctor/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Doctor doctor = db.Doctors.Find(id);

            if (doctor == null)
                return HttpNotFound();

            return View(doctor);
        }

        // GET: Doctor/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Doctor doctor = db.Doctors.Find(id);

            if (doctor == null)
                return HttpNotFound();

            LoadSpecialties(doctor.SpecialtyID);
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Doctor doctor)
        {
            var existingDoctor = db.Doctors.Find(doctor.DoctorID);

            if (existingDoctor == null)
                return HttpNotFound();

            if (!doctor.SpecialtyID.HasValue)
            {
                ModelState.AddModelError(
                    "SpecialtyID",
                    "Vui lòng chọn chuyên khoa"
                );
            }
            else
            {
                var specialty = db.Specialties.Find(doctor.SpecialtyID);

                if (specialty == null)
                {
                    ModelState.AddModelError(
                        "SpecialtyID",
                        "Chuyên khoa không tồn tại"
                    );
                }
                else
                {
                    doctor.Specialty = specialty.SpecialtyName;
                }
            }

            if (ModelState.IsValid)
            {
                existingDoctor.DoctorCode = doctor.DoctorCode;
                existingDoctor.FullName = doctor.FullName;
                existingDoctor.Phone = doctor.Phone;
                existingDoctor.Email = doctor.Email;
                existingDoctor.Address = doctor.Address;
                existingDoctor.IsActive = doctor.IsActive;

                existingDoctor.SpecialtyID = doctor.SpecialtyID;
                existingDoctor.Specialty = doctor.Specialty;

                db.SaveChanges();

                ActivityLogHelper.Write(
                    "Cập nhật",
                    "Bác sĩ",
                    "Cập nhật thông tin bác sĩ: " +
                    existingDoctor.FullName
                );

                TempData["Success"] = "Cập nhật bác sĩ thành công.";

                return RedirectToAction("Index");
            }

            LoadSpecialties(doctor.SpecialtyID);
            return View(doctor);
        }

        // GET: Doctor/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Doctor doctor = db.Doctors.Find(id);

            if (doctor == null)
                return HttpNotFound();

            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Doctor doctor = db.Doctors.Find(id);

            if (doctor == null)
                return HttpNotFound();

            string doctorName = doctor.FullName;

            db.Doctors.Remove(doctor);
            db.SaveChanges();

            ActivityLogHelper.Write(
                "Xóa",
                "Bác sĩ",
                "Xóa bác sĩ: " + doctorName
            );

            TempData["Success"] = "Xóa bác sĩ thành công.";

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
