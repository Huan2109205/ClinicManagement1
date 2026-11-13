
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class ProfileController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // GET: Profile
        public ActionResult Index()
        {
            var user = db.Users
                .Include(u => u.Doctor)
                .FirstOrDefault(u => u.Username == User.Identity.Name);

            if (user == null || !user.DoctorID.HasValue || user.Doctor == null)
            {
                return HttpNotFound("Tài khoản chưa được liên kết với hồ sơ bác sĩ.");
            }

            var doctor = user.Doctor;

            var model = new DoctorProfileViewModel
            {
                DoctorCode = doctor.DoctorCode,
                FullName = doctor.FullName,
                SpecialtyName = doctor.SpecialtyNavigation != null
                    ? doctor.SpecialtyNavigation.SpecialtyName
                    : doctor.Specialty,
                Phone = doctor.Phone,
                Email = doctor.Email,
                Address = doctor.Address
            };

            return View(model);
        }

        // POST: Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(DoctorProfileViewModel model)
        {
            var user = db.Users
                .FirstOrDefault(u => u.Username == User.Identity.Name);

            if (user == null || !user.DoctorID.HasValue)
            {
                return HttpNotFound();
            }

            var doctor = db.Doctors.Find(user.DoctorID.Value);

            if (doctor == null)
            {
                return HttpNotFound();
            }

            // Giữ nguyên các thông tin không được phép sửa
            model.DoctorCode = doctor.DoctorCode;
            model.FullName = doctor.FullName;

            var specialty = doctor.SpecialtyID.HasValue
                ? db.Specialties.Find(doctor.SpecialtyID.Value)
                : null;

            model.SpecialtyName = specialty != null
                ? specialty.SpecialtyName
                : doctor.Specialty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            doctor.Phone = model.Phone;
            doctor.Email = model.Email;
            doctor.Address = model.Address;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật hồ sơ thành công!";

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
