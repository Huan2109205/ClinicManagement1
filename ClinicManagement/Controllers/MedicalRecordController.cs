
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;
using ClinicManagement.Helpers;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist,Doctor")]
    public class MedicalRecordController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // DANH SÁCH HỒ SƠ
        public ActionResult Index(string keyword)
        {
            var records = db.MedicalRecords
                .Include(r => r.Appointment.Patient)
                .Include(r => r.Appointment.Doctor)
                .AsQueryable();

            // Bác sĩ chỉ xem hồ sơ của mình
            if (User.IsInRole("Doctor"))
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name);

                if (user == null || !user.DoctorID.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                int doctorID = user.DoctorID.Value;

                records = records.Where(
                    r => r.Appointment.DoctorID == doctorID);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                records = records.Where(r =>
                    r.RecordCode.Contains(keyword) ||
                    r.Appointment.Patient.FullName.Contains(keyword) ||
                    r.Appointment.Patient.PatientCode.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(records.OrderByDescending(
                r => r.RecordDate).ToList());
        }

        // GET: MedicalRecord/Create
        [Authorize(Roles = "Admin,Manager,Doctor")]
        public ActionResult Create()
        {
            LoadAppointments();
            return View();
        }

        // POST: MedicalRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Doctor")]
        public ActionResult Create(MedicalRecord record)
        {
            if (User.IsInRole("Doctor"))
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name);

                if (user == null || !user.DoctorID.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                bool isOwnAppointment = db.Appointments.Any(a =>
                    a.AppointmentID == record.AppointmentID &&
                    a.DoctorID == user.DoctorID.Value);

                if (!isOwnAppointment)
                {
                    ModelState.AddModelError(
                        "AppointmentID",
                        "Bạn chỉ được lập hồ sơ cho lịch khám của mình.");
                }
            }

            if (db.MedicalRecords.Any(r =>
                r.RecordCode == record.RecordCode))
            {
                ModelState.AddModelError(
                    "RecordCode",
                    "Mã hồ sơ đã tồn tại.");
            }

            // Kiểm tra lịch khám đã có hồ sơ chưa
            if (db.MedicalRecords.Any(r =>
                r.AppointmentID == record.AppointmentID))
            {
                ModelState.AddModelError(
                    "AppointmentID",
                    "Lịch khám này đã có hồ sơ bệnh án.");
            }

            // Chỉ lập hồ sơ cho lịch khám hoàn thành
            bool appointmentExists = db.Appointments.Any(a =>
                a.AppointmentID == record.AppointmentID &&
                a.Status == "Hoàn thành");

            if (!appointmentExists)
            {
                ModelState.AddModelError(
                    "AppointmentID",
                    "Chỉ được lập hồ sơ cho lịch khám đã hoàn thành.");
            }

            if (!ModelState.IsValid)
            {
                LoadAppointments(record.AppointmentID);
                return View(record);
            }

            record.RecordDate = DateTime.Now;

            db.MedicalRecords.Add(record);
            db.SaveChanges();

            // GHI NHẬT KÝ TẠO HỒ SƠ
            ActivityLogHelper.Write(
                "Thêm mới",
                "Bệnh án",
                "Tạo hồ sơ bệnh án mã: " + record.RecordCode +
                ", mã lịch khám: " + record.AppointmentID
            );

            TempData["Success"] = "Tạo hồ sơ khám bệnh thành công.";

            return RedirectToAction("Index");
        }

        // CHI TIẾT HỒ SƠ
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var record = db.MedicalRecords
                .Include(r => r.Appointment.Patient)
                .Include(r => r.Appointment.Doctor)
                .FirstOrDefault(r => r.RecordID == id);

            if (record == null)
                return HttpNotFound();

            // Bác sĩ chỉ xem hồ sơ của mình
            if (User.IsInRole("Doctor"))
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name);

                if (user == null ||
                    !user.DoctorID.HasValue ||
                    record.Appointment.DoctorID != user.DoctorID.Value)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }

            return View(record);
        }

        // NẠP DANH SÁCH LỊCH KHÁM
        private void LoadAppointments(int? selectedID = null)
        {
            var appointments = db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.Status == "Hoàn thành");

            if (User.IsInRole("Doctor"))
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name);

                if (user == null || !user.DoctorID.HasValue)
                {
                    ViewBag.AppointmentID =
                        new SelectList(
                            Enumerable.Empty<Appointment>(),
                            "AppointmentID",
                            "AppointmentCode");
                    return;
                }

                int doctorID = user.DoctorID.Value;

                appointments = appointments.Where(
                    a => a.DoctorID == doctorID);
            }

            ViewBag.AppointmentID = new SelectList(
                appointments.ToList().Select(a => new
                {
                    a.AppointmentID,
                    Display = a.AppointmentCode + " - " +
                              a.Patient.FullName
                }),
                "AppointmentID",
                "Display",
                selectedID);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
