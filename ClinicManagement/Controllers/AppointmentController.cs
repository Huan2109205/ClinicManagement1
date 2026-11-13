
using System;
using System.Linq;
using System.Net;
using System.Data.Entity;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;
using ClinicManagement.Helpers;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist,Doctor")]
    public class AppointmentController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // DANH SACH TRANG THAI LICH KHAM
        private readonly string[] statuses =
        {
            "Chờ xác nhận",
            "Đã xác nhận",
            "Đang khám",
            "Hoàn thành",
            "Đã hủy"
        };

        // LAY DOCTORID CUA TAI KHOAN DANG DANG NHAP
        private int? GetCurrentDoctorID()
        {
            return db.Users
                .Where(u =>
                    u.Username == User.Identity.Name &&
                    u.IsActive)
                .Select(u => u.DoctorID)
                .FirstOrDefault();
        }

        // NAP DANH SACH BENH NHAN, BAC SI VA TRANG THAI
        private void LoadDropdowns(
            int? patientId = null,
            int? doctorId = null,
            string status = null)
        {
            ViewBag.PatientID = new SelectList(
                db.Patients.OrderBy(p => p.FullName).ToList(),
                "PatientID",
                "FullName",
                patientId);

            ViewBag.DoctorID = new SelectList(
                db.Doctors.OrderBy(d => d.FullName).ToList(),
                "DoctorID",
                "FullName",
                doctorId);

            ViewBag.Status = new SelectList(
                statuses,
                status);
        }

        // KIEM TRA TRUNG LICH CUA BAC SI
        private bool IsDoctorBusy(
            int doctorId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            int? currentAppointmentId = null)
        {
            return db.Appointments.Any(a =>
                a.DoctorID == doctorId &&
                DbFunctions.TruncateTime(a.AppointmentDate)
                    == appointmentDate.Date &&
                a.AppointmentTime == appointmentTime &&
                a.Status != "Đã hủy" &&
                (!currentAppointmentId.HasValue ||
                    a.AppointmentID != currentAppointmentId.Value));
        }

        // GET: Appointment
        public ActionResult Index(string keyword, string status)
        {
            var appointments = db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            // DOCTOR CHI XEM LICH CUA CHINH MINH
            if (User.IsInRole("Doctor"))
            {
                int? doctorId = GetCurrentDoctorID();

                if (!doctorId.HasValue)
                {
                    return new HttpStatusCodeResult(
                        HttpStatusCode.Forbidden,
                        "Tài khoản bác sĩ chưa được liên kết với hồ sơ bác sĩ.");
                }

                appointments = appointments.Where(
                    a => a.DoctorID == doctorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                appointments = appointments.Where(a =>
                    a.AppointmentCode.Contains(keyword) ||
                    a.Patient.FullName.Contains(keyword) ||
                    a.Doctor.FullName.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                appointments = appointments.Where(
                    a => a.Status == status);
            }

            ViewBag.Keyword = keyword;
            ViewBag.SelectedStatus = status;
            ViewBag.StatusList = new SelectList(statuses, status);

            return View(appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList());
        }

        // GET: Appointment/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Appointment appointment = db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.AppointmentID == id);

            if (appointment == null)
                return HttpNotFound();

            // KHONG CHO DOCTOR XEM LICH CUA BAC SI KHAC
            if (User.IsInRole("Doctor"))
            {
                int? doctorId = GetCurrentDoctorID();

                if (!doctorId.HasValue)
                {
                    return new HttpStatusCodeResult(
                        HttpStatusCode.Forbidden);
                }

                if (appointment.DoctorID != doctorId.Value)
                    return HttpNotFound();
            }

            return View(appointment);
        }

        // GET: Appointment/Create
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public ActionResult Create()
        {
            LoadDropdowns();

            Appointment appointment = new Appointment
            {
                AppointmentDate = DateTime.Today,
                AppointmentTime = new TimeSpan(8, 0, 0),
                Status = "Chờ xác nhận"
            };

            return View(appointment);
        }

        // POST: Appointment/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Appointment appointment)
        {
            if (appointment.AppointmentCode != null)
                appointment.AppointmentCode =
                    appointment.AppointmentCode.Trim();

            if (appointment.Reason != null)
                appointment.Reason = appointment.Reason.Trim();

            if (appointment.Note != null)
                appointment.Note = appointment.Note.Trim();

            // KIEM TRA MA LICH KHAM
            bool codeExists =
                !string.IsNullOrWhiteSpace(appointment.AppointmentCode)
                && db.Appointments.Any(a =>
                    a.AppointmentCode == appointment.AppointmentCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "AppointmentCode",
                    "Mã lịch khám đã tồn tại.");
            }

            // KIEM TRA BENH NHAN
            if (db.Patients.Find(appointment.PatientID) == null)
            {
                ModelState.AddModelError(
                    "PatientID",
                    "Bệnh nhân không tồn tại.");
            }

            // KIEM TRA BAC SI
            if (db.Doctors.Find(appointment.DoctorID) == null)
            {
                ModelState.AddModelError(
                    "DoctorID",
                    "Bác sĩ không tồn tại.");
            }

            // KIEM TRA TRANG THAI
            if (string.IsNullOrWhiteSpace(appointment.Status))
            {
                appointment.Status = "Chờ xác nhận";
            }
            else if (!statuses.Contains(appointment.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Trạng thái lịch khám không hợp lệ.");
            }

            // KIEM TRA TRUNG GIO BAC SI
            if (ModelState.IsValid &&
                IsDoctorBusy(
                    appointment.DoctorID,
                    appointment.AppointmentDate,
                    appointment.AppointmentTime))
            {
                ModelState.AddModelError(
                    "",
                    "Bác sĩ đã có lịch khám vào ngày và giờ này.");
            }

            if (ModelState.IsValid)
            {
                db.Appointments.Add(appointment);
                db.SaveChanges();

                ActivityLogHelper.Write(
                    "Thêm mới",
                    "Lịch khám",
                    "Thêm lịch khám mã: " +
                    appointment.AppointmentCode
                );

                TempData["Success"] =
                    "Thêm lịch khám thành công.";

                return RedirectToAction("Index");
            }

            LoadDropdowns(
                appointment.PatientID,
                appointment.DoctorID,
                appointment.Status);

            return View(appointment);
        }

        // GET: Appointment/Edit/5
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Appointment appointment = db.Appointments.Find(id);

            if (appointment == null)
                return HttpNotFound();

            LoadDropdowns(
                appointment.PatientID,
                appointment.DoctorID,
                appointment.Status);

            return View(appointment);
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Appointment appointment)
        {
            Appointment existing = db.Appointments
                .Find(appointment.AppointmentID);

            if (existing == null)
                return HttpNotFound();

            if (appointment.AppointmentCode != null)
                appointment.AppointmentCode =
                    appointment.AppointmentCode.Trim();

            if (appointment.Reason != null)
                appointment.Reason = appointment.Reason.Trim();

            if (appointment.Note != null)
                appointment.Note = appointment.Note.Trim();

            // KIEM TRA MA LICH KHAM TRUNG
            bool codeExists =
                !string.IsNullOrWhiteSpace(appointment.AppointmentCode)
                && db.Appointments.Any(a =>
                    a.AppointmentCode == appointment.AppointmentCode &&
                    a.AppointmentID != appointment.AppointmentID);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "AppointmentCode",
                    "Mã lịch khám đã được sử dụng.");
            }

            // KIEM TRA BENH NHAN
            if (db.Patients.Find(appointment.PatientID) == null)
            {
                ModelState.AddModelError(
                    "PatientID",
                    "Bệnh nhân không tồn tại.");
            }

            // KIEM TRA BAC SI
            if (db.Doctors.Find(appointment.DoctorID) == null)
            {
                ModelState.AddModelError(
                    "DoctorID",
                    "Bác sĩ không tồn tại.");
            }

            // KIEM TRA TRANG THAI
            if (string.IsNullOrWhiteSpace(appointment.Status) ||
                !statuses.Contains(appointment.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Vui lòng chọn trạng thái hợp lệ.");
            }

            // KIEM TRA TRUNG GIO BAC SI
            if (ModelState.IsValid &&
                IsDoctorBusy(
                    appointment.DoctorID,
                    appointment.AppointmentDate,
                    appointment.AppointmentTime,
                    appointment.AppointmentID))
            {
                ModelState.AddModelError(
                    "",
                    "Bác sĩ đã có lịch khám vào ngày và giờ này.");
            }

            if (ModelState.IsValid)
            {
                // LUU THONG TIN CU DE GHI NHAT KY
                string oldStatus = existing.Status;
                string appointmentCode = existing.AppointmentCode;

                existing.AppointmentCode = appointment.AppointmentCode;
                existing.PatientID = appointment.PatientID;
                existing.DoctorID = appointment.DoctorID;
                existing.AppointmentDate = appointment.AppointmentDate;
                existing.AppointmentTime = appointment.AppointmentTime;
                existing.Reason = appointment.Reason;
                existing.Status = appointment.Status;
                existing.Note = appointment.Note;

                db.SaveChanges();

                string description =
                    "Cập nhật lịch khám mã: " + appointmentCode;

                if (oldStatus != appointment.Status)
                {
                    description +=
                        ". Thay đổi trạng thái từ '" +
                        oldStatus + "' thành '" +
                        appointment.Status + "'";
                }

                ActivityLogHelper.Write(
                    "Cập nhật",
                    "Lịch khám",
                    description
                );

                // GHI RIENG KHI THAY DOI TRANG THAI
                if (oldStatus != appointment.Status)
                {
                    ActivityLogHelper.Write(
                        "Thay đổi trạng thái",
                        "Lịch khám",
                        "Lịch khám mã " + appointmentCode +
                        ": " + oldStatus +
                        " -> " + appointment.Status
                    );
                }

                TempData["Success"] =
                    "Cập nhật lịch khám thành công.";

                return RedirectToAction("Index");
            }

            LoadDropdowns(
                appointment.PatientID,
                appointment.DoctorID,
                appointment.Status);

            return View(appointment);
        }

        // GET: Appointment/Delete/5
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Appointment appointment = db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.AppointmentID == id);

            if (appointment == null)
                return HttpNotFound();

            return View(appointment);
        }

        // POST: Appointment/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Appointment appointment =
                db.Appointments.Find(id);

            if (appointment == null)
                return HttpNotFound();

            string appointmentCode = appointment.AppointmentCode;

            db.Appointments.Remove(appointment);
            db.SaveChanges();

            ActivityLogHelper.Write(
                "Xóa",
                "Lịch khám",
                "Xóa lịch khám mã: " + appointmentCode
            );

            TempData["Success"] =
                "Xóa lịch khám thành công.";

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
