
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Models;
using ClinicManagement.Helpers;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Doctor")]
    public class PrescriptionController : Controller
    {
        private ClinicDbContext db = new ClinicDbContext();

        // DANH SÁCH ĐƠN THUỐC
        public ActionResult Index(string keyword)
        {
            var prescriptions = db.Prescriptions
                .Include(p => p.MedicalRecord.Appointment.Patient)
                .Include(p => p.MedicalRecord.Appointment.Doctor)
                .Include(p => p.Details)
                .AsQueryable();

            // Bác sĩ chỉ xem đơn thuốc thuộc hồ sơ của mình
            if (User.IsInRole("Doctor"))
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name);

                if (user == null || !user.DoctorID.HasValue)
                    return new HttpStatusCodeResult(403);

                int doctorID = user.DoctorID.Value;

                prescriptions = prescriptions.Where(p =>
                    p.MedicalRecord.Appointment.DoctorID == doctorID);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                prescriptions = prescriptions.Where(p =>
                    p.PrescriptionCode.Contains(keyword) ||
                    p.MedicalRecord.RecordCode.Contains(keyword) ||
                    p.MedicalRecord.Appointment.Patient.FullName.Contains(keyword));
            }

            return View(prescriptions
                .OrderByDescending(p => p.PrescriptionDate)
                .ToList());
        }

        // TẠO ĐƠN THUỐC
        [Authorize(Roles = "Admin,Manager,Doctor")]
        public ActionResult Create()
        {
            LoadMedicalRecords();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Doctor")]
        public ActionResult Create(Prescription prescription)
        {
            prescription.PrescriptionDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(prescription.PrescriptionCode))
            {
                ModelState.AddModelError(
                    "PrescriptionCode", "Vui lòng nhập mã đơn thuốc");
            }
            else
            {
                prescription.PrescriptionCode =
                    prescription.PrescriptionCode.Trim();

                if (db.Prescriptions.Any(p =>
                    p.PrescriptionCode == prescription.PrescriptionCode))
                {
                    ModelState.AddModelError(
                        "PrescriptionCode", "Mã đơn thuốc đã tồn tại");
                }
            }

            var record = db.MedicalRecords
                .Include(r => r.Appointment)
                .FirstOrDefault(r => r.RecordID == prescription.RecordID);

            if (record == null)
            {
                ModelState.AddModelError(
                    "RecordID", "Hồ sơ bệnh án không tồn tại");
            }
            else
            {
                // Mỗi hồ sơ chỉ có một đơn thuốc
                if (db.Prescriptions.Any(p =>
                    p.RecordID == prescription.RecordID))
                {
                    ModelState.AddModelError(
                        "RecordID", "Hồ sơ này đã có đơn thuốc");
                }

                // Bác sĩ chỉ được kê đơn cho bệnh nhân của mình
                if (User.IsInRole("Doctor"))
                {
                    var user = db.Users.FirstOrDefault(
                        u => u.Username == User.Identity.Name);

                    if (user == null ||
                        !user.DoctorID.HasValue ||
                        record.Appointment.DoctorID != user.DoctorID.Value)
                    {
                        return new HttpStatusCodeResult(403);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                LoadMedicalRecords(prescription.RecordID);
                return View(prescription);
            }

            prescription.Details =
                new System.Collections.Generic.List<PrescriptionDetail>();

            db.Prescriptions.Add(prescription);
            db.SaveChanges();

            // GHI NHẬT KÝ TẠO ĐƠN THUỐC
            ActivityLogHelper.Write(
                "Thêm mới",
                "Kê đơn thuốc",
                "Tạo đơn thuốc mã: " + prescription.PrescriptionCode +
                ", mã hồ sơ: " + prescription.RecordID
            );

            TempData["Success"] =
                "Tạo đơn thuốc thành công! Hãy bổ sung thuốc vào đơn.";

            return RedirectToAction("Details",
                new { id = prescription.PrescriptionID });
        }

        // XEM CHI TIẾT ĐƠN THUỐC
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(400);

            var prescription = db.Prescriptions
                .Include(p => p.MedicalRecord.Appointment.Patient)
                .Include(p => p.MedicalRecord.Appointment.Doctor)
                .Include(p => p.Details.Select(d => d.Medicine))
                .FirstOrDefault(p => p.PrescriptionID == id);

            if (prescription == null)
                return HttpNotFound();

            if (!CanAccessPrescription(prescription))
                return new HttpStatusCodeResult(403);

            ViewBag.Medicines = db.Medicines
                .Where(m => m.IsActive)
                .OrderBy(m => m.MedicineName)
                .ToList();

            return View(prescription);
        }

        // THÊM THUỐC VÀO ĐƠN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMedicine(
            int prescriptionID,
            int medicineID,
            int quantity,
            string dosage,
            string frequency,
            int? durationDays,
            string instructions)
        {
            var prescription = db.Prescriptions
                .Include(p => p.MedicalRecord.Appointment)
                .FirstOrDefault(p => p.PrescriptionID == prescriptionID);

            if (prescription == null)
                return HttpNotFound();

            if (!CanAccessPrescription(prescription))
                return new HttpStatusCodeResult(403);

            var medicine = db.Medicines.FirstOrDefault(
                m => m.MedicineID == medicineID && m.IsActive);

            if (medicine == null)
                ModelState.AddModelError(
                    "", "Thuốc không tồn tại hoặc đã ngừng sử dụng");

            if (quantity < 1 || quantity > 10000)
                ModelState.AddModelError(
                    "", "Số lượng phải từ 1 đến 10000");

            if (string.IsNullOrWhiteSpace(dosage))
                ModelState.AddModelError(
                    "", "Vui lòng nhập liều dùng");

            if (durationDays.HasValue &&
                (durationDays.Value < 1 || durationDays.Value > 365))
            {
                ModelState.AddModelError(
                    "", "Số ngày sử dụng không hợp lệ");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin thuốc không hợp lệ.";
                return RedirectToAction("Details",
                    new { id = prescriptionID });
            }

            decimal lineTotal = medicine.UnitPrice * quantity;

            var detail = new PrescriptionDetail
            {
                PrescriptionID = prescriptionID,
                MedicineID = medicineID,
                Quantity = quantity,
                Dosage = dosage.Trim(),
                Frequency = frequency,
                DurationDays = durationDays,
                Instructions = instructions,
                UnitPrice = medicine.UnitPrice,
                LineTotal = lineTotal
            };

            db.PrescriptionDetails.Add(detail);
            db.SaveChanges();

            // GHI NHẬT KÝ THÊM THUỐC
            ActivityLogHelper.Write(
                "Thêm thuốc",
                "Kê đơn thuốc",
                "Thêm thuốc " + medicine.MedicineName +
                " vào đơn " + prescription.PrescriptionCode +
                ", số lượng: " + quantity +
                ", thành tiền: " + lineTotal
            );

            TempData["Success"] = "Đã thêm thuốc vào đơn!";

            return RedirectToAction("Details",
                new { id = prescriptionID });
        }

        // XÓA THUỐC KHỎI ĐƠN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveMedicine(int id)
        {
            var detail = db.PrescriptionDetails
                .Include(d => d.Prescription.MedicalRecord.Appointment)
                .Include(d => d.Medicine)
                .FirstOrDefault(d => d.PrescriptionDetailID == id);

            if (detail == null)
                return HttpNotFound();

            var prescription = detail.Prescription;

            if (!CanAccessPrescription(prescription))
                return new HttpStatusCodeResult(403);

            int prescriptionID = prescription.PrescriptionID;

            // Lưu thông tin trước khi xóa
            string prescriptionCode = prescription.PrescriptionCode;
            string medicineName = detail.Medicine != null
                ? detail.Medicine.MedicineName
                : "Không xác định";

            int quantity = detail.Quantity;

            db.PrescriptionDetails.Remove(detail);
            db.SaveChanges();

            // GHI NHẬT KÝ XÓA THUỐC
            ActivityLogHelper.Write(
                "Xóa thuốc",
                "Kê đơn thuốc",
                "Xóa thuốc " + medicineName +
                " khỏi đơn " + prescriptionCode +
                ", số lượng: " + quantity
            );

            TempData["Success"] = "Đã xóa thuốc khỏi đơn!";

            return RedirectToAction("Details",
                new { id = prescriptionID });
        }

        // KIỂM TRA QUYỀN XEM VÀ THAO TÁC ĐƠN THUỐC
        private bool CanAccessPrescription(Prescription prescription)
        {
            if (!User.IsInRole("Doctor"))
                return true;

            var user = db.Users.FirstOrDefault(
                u => u.Username == User.Identity.Name);

            return user != null &&
                   user.DoctorID.HasValue &&
                   prescription.MedicalRecord.Appointment.DoctorID
                       == user.DoctorID.Value;
        }

        // NẠP DANH SÁCH HỒ SƠ BỆNH ÁN
        private void LoadMedicalRecords(int? selectedID = null)
        {
            var records = db.MedicalRecords
                .Include(r => r.Appointment.Patient)
                .Include(r => r.Appointment.Doctor)
                .Where(r => !db.Prescriptions.Any(
                    p => p.RecordID == r.RecordID));

            if (User.IsInRole("Doctor"))
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == User.Identity.Name);

                if (user == null || !user.DoctorID.HasValue)
                {
                    ViewBag.RecordID = new SelectList(
                        new object[0], "RecordID", "RecordCode");
                    return;
                }

                int doctorID = user.DoctorID.Value;

                records = records.Where(r =>
                    r.Appointment.DoctorID == doctorID);
            }

            var list = records.ToList().Select(r => new
            {
                r.RecordID,
                Display = r.RecordCode + " - " +
                    r.Appointment.Patient.FullName + " - " +
                    r.Appointment.Doctor.FullName
            });

            ViewBag.RecordID = new SelectList(
                list, "RecordID", "Display", selectedID);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
