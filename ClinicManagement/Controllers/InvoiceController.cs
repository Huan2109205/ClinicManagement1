
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ClinicManagement.DAL;
using ClinicManagement.Helpers;
using ClinicManagement.Models;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public class InvoiceController : Controller
    {
        private readonly ClinicDbContext db = new ClinicDbContext();

        private readonly List<string> paymentStatuses = new List<string>
        {
            "Chưa thanh toán",
            "Thanh toán một phần",
            "Đã thanh toán",
            "Đã hủy"
        };

        private readonly List<string> paymentMethods = new List<string>
        {
            "Tiền mặt",
            "Chuyển khoản",
            "VNPay",
            "Thẻ"
        };

        // TÍNH TIỀN THUỐC THEO LỊCH KHÁM
        private decimal GetMedicineAmount(int appointmentID)
        {
            var recordIDs = db.MedicalRecords
                .Where(r => r.AppointmentID == appointmentID)
                .Select(r => r.RecordID);

            return db.PrescriptionDetails
                .Where(d => recordIDs.Contains(d.Prescription.RecordID))
                .Select(d => (decimal?)d.LineTotal)
                .Sum() ?? 0m;
        }

        // XỬ LÝ HÓA ĐƠN CŨ ĐÃ THANH TOÁN NHƯNG PaidAmount = 0
        private decimal GetActualPaidAmount(Invoice invoice)
        {
            if (invoice == null)
                return 0m;

            if (invoice.PaymentStatus == "Đã hủy")
                return 0m;

            if (invoice.PaymentStatus == "Đã thanh toán" &&
                invoice.PaidAmount <= 0)
            {
                return invoice.TotalAmount;
            }

            return Math.Max(
                0m,
                Math.Min(invoice.PaidAmount, invoice.TotalAmount)
            );
        }

        // TỰ ĐỘNG XÁC ĐỊNH TRẠNG THÁI
        private string GetPaymentStatus(decimal paidAmount, decimal totalAmount)
        {
            if (paidAmount <= 0)
                return "Chưa thanh toán";

            if (paidAmount >= totalAmount)
                return "Đã thanh toán";

            return "Thanh toán một phần";
        }

        // NẠP DROPDOWN
        private void LoadDropdowns(
            int? selectedAppointmentId = null,
            string selectedPaymentMethod = null,
            string selectedPaymentStatus = null)
        {
            var appointments = db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.Status == null || a.Status != "Đã hủy")
                .Where(a =>
                    !db.Invoices.Any(i =>
                        i.AppointmentID == a.AppointmentID)
                    || a.AppointmentID == selectedAppointmentId)
                .ToList();

            ViewBag.AppointmentID = new SelectList(
                appointments.Select(a => new SelectListItem
                {
                    Value = a.AppointmentID.ToString(),
                    Text = a.AppointmentCode + " - " +
                        (a.Patient != null ? a.Patient.FullName : "") +
                        " - " + a.AppointmentDate.ToString("dd/MM/yyyy")
                }),
                "Value",
                "Text",
                selectedAppointmentId
            );

            ViewBag.PaymentMethod = new SelectList(
                paymentMethods.Select(x => new SelectListItem
                {
                    Value = x,
                    Text = x
                }),
                "Value",
                "Text",
                selectedPaymentMethod
            );

            ViewBag.PaymentStatus = new SelectList(
                paymentStatuses.Select(x => new SelectListItem
                {
                    Value = x,
                    Text = x
                }),
                "Value",
                "Text",
                selectedPaymentStatus
            );
        }

        // DANH SÁCH HÓA ĐƠN
        public ActionResult Index(string keyword, string paymentStatus)
        {
            var invoices = db.Invoices
                .Include(i => i.Appointment.Patient)
                .Include(i => i.Appointment.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                invoices = invoices.Where(i =>
                    i.InvoiceCode.Contains(keyword) ||
                    i.Appointment.AppointmentCode.Contains(keyword) ||
                    i.Appointment.Patient.FullName.Contains(keyword) ||
                    i.Appointment.Doctor.FullName.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                invoices = invoices.Where(i =>
                    i.PaymentStatus == paymentStatus);
            }

            ViewBag.Keyword = keyword;
            ViewBag.PaymentStatusFilter = paymentStatus;

            return View(
                invoices.OrderByDescending(i => i.InvoiceDate).ToList()
            );
        }

        // GET: Invoice/Create
        public ActionResult Create()
        {
            LoadDropdowns();

            return View(new Invoice
            {
                InvoiceDate = DateTime.Today,
                PaymentStatus = "Chưa thanh toán",
                PaidAmount = 0m,
                ConsultationFee = 0m,
                MedicineAmount = 0m,
                TotalAmount = 0m
            });
        }

        // POST: Invoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Invoice invoice)
        {
            // Các trường này được tính ở phía máy chủ
            invoice.PaidAmount = 0m;
            invoice.PaymentStatus = "Chưa thanh toán";

            ModelState.Remove("PaidAmount");
            ModelState.Remove("MedicineAmount");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("PaymentStatus");

            // KIỂM TRA MÃ HÓA ĐƠN
            if (string.IsNullOrWhiteSpace(invoice.InvoiceCode))
            {
                ModelState.AddModelError(
                    "InvoiceCode",
                    "Vui lòng nhập mã hóa đơn"
                );
            }
            else
            {
                invoice.InvoiceCode = invoice.InvoiceCode.Trim();

                if (db.Invoices.Any(i =>
                    i.InvoiceCode == invoice.InvoiceCode))
                {
                    ModelState.AddModelError(
                        "InvoiceCode",
                        "Mã hóa đơn đã tồn tại"
                    );
                }
            }

            // KIỂM TRA PHÍ KHÁM
            if (invoice.ConsultationFee < 0)
            {
                ModelState.AddModelError(
                    "ConsultationFee",
                    "Phí khám không được nhỏ hơn 0"
                );
            }

            // KIỂM TRA LỊCH KHÁM
            var appointment = db.Appointments
                .FirstOrDefault(a =>
                    a.AppointmentID == invoice.AppointmentID);

            if (appointment == null)
            {
                ModelState.AddModelError(
                    "AppointmentID",
                    "Lịch khám không tồn tại"
                );
            }
            else
            {
                if (appointment.Status == "Đã hủy")
                {
                    ModelState.AddModelError(
                        "AppointmentID",
                        "Không thể lập hóa đơn cho lịch khám đã hủy"
                    );
                }

                if (db.Invoices.Any(i =>
                    i.AppointmentID == invoice.AppointmentID))
                {
                    ModelState.AddModelError(
                        "AppointmentID",
                        "Lịch khám này đã có hóa đơn"
                    );
                }
            }

            // PHƯƠNG THỨC THANH TOÁN KHI TẠO HÓA ĐƠN LÀ TÙY CHỌN
            if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod) &&
                !paymentMethods.Contains(invoice.PaymentMethod))
            {
                ModelState.AddModelError(
                    "PaymentMethod",
                    "Phương thức thanh toán không hợp lệ"
                );
            }

            if (ModelState.IsValid)
            {
                invoice.MedicineAmount =
                    GetMedicineAmount(invoice.AppointmentID);

                invoice.TotalAmount =
                    invoice.ConsultationFee + invoice.MedicineAmount;

                invoice.PaidAmount = 0m;
                invoice.PaymentStatus = "Chưa thanh toán";

                db.Invoices.Add(invoice);
                db.SaveChanges();

                ActivityLogHelper.Write(
                    "Thêm mới",
                    "Hóa đơn",
                    "Thêm hóa đơn mã: " + invoice.InvoiceCode +
                    ", tổng tiền: " + invoice.TotalAmount.ToString("N0") +
                    " VNĐ"
                );

                TempData["Success"] = "Thêm hóa đơn thành công!";

                return RedirectToAction("Index");
            }

            LoadDropdowns(
                invoice.AppointmentID,
                invoice.PaymentMethod,
                "Chưa thanh toán"
            );

            return View(invoice);
        }

        // GET: Invoice/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var invoice = db.Invoices
                .Include(i => i.Appointment.Patient)
                .Include(i => i.Appointment.Doctor)
                .FirstOrDefault(i => i.InvoiceID == id);

            if (invoice == null)
                return HttpNotFound();

            return View(invoice);
        }

        // IN HÓA ĐƠN
        public ActionResult Print(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var invoice = db.Invoices
                .Include(i => i.Appointment.Patient)
                .Include(i => i.Appointment.Doctor)
                .FirstOrDefault(i => i.InvoiceID == id);

            if (invoice == null)
                return HttpNotFound();

            return View(invoice);
        }

        // GET: Invoice/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var invoice = db.Invoices.Find(id);

            if (invoice == null)
                return HttpNotFound();

            LoadDropdowns(
                invoice.AppointmentID,
                invoice.PaymentMethod,
                invoice.PaymentStatus
            );

            return View(invoice);
        }

        // POST: Invoice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Invoice invoice)
        {
            var existingInvoice = db.Invoices.Find(invoice.InvoiceID);

            if (existingInvoice == null)
                return HttpNotFound();

            // Các trường này không được lấy trực tiếp từ form
            ModelState.Remove("PaidAmount");
            ModelState.Remove("MedicineAmount");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("PaymentStatus");

            // KIỂM TRA MÃ HÓA ĐƠN
            if (string.IsNullOrWhiteSpace(invoice.InvoiceCode))
            {
                ModelState.AddModelError(
                    "InvoiceCode",
                    "Vui lòng nhập mã hóa đơn"
                );
            }
            else
            {
                invoice.InvoiceCode = invoice.InvoiceCode.Trim();

                if (db.Invoices.Any(i =>
                    i.InvoiceCode == invoice.InvoiceCode &&
                    i.InvoiceID != invoice.InvoiceID))
                {
                    ModelState.AddModelError(
                        "InvoiceCode",
                        "Mã hóa đơn đã tồn tại"
                    );
                }
            }

            // KIỂM TRA PHÍ KHÁM
            if (invoice.ConsultationFee < 0)
            {
                ModelState.AddModelError(
                    "ConsultationFee",
                    "Phí khám không được nhỏ hơn 0"
                );
            }

            // KIỂM TRA LỊCH KHÁM
            var appointment = db.Appointments
                .FirstOrDefault(a =>
                    a.AppointmentID == invoice.AppointmentID);

            if (appointment == null)
            {
                ModelState.AddModelError(
                    "AppointmentID",
                    "Lịch khám không tồn tại"
                );
            }
            else
            {
                if (appointment.Status == "Đã hủy")
                {
                    ModelState.AddModelError(
                        "AppointmentID",
                        "Không thể lập hóa đơn cho lịch khám đã hủy"
                    );
                }

                if (db.Invoices.Any(i =>
                    i.AppointmentID == invoice.AppointmentID &&
                    i.InvoiceID != invoice.InvoiceID))
                {
                    ModelState.AddModelError(
                        "AppointmentID",
                        "Lịch khám này đã có hóa đơn khác"
                    );
                }
            }

            // KIỂM TRA PHƯƠNG THỨC THANH TOÁN
            if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod) &&
                !paymentMethods.Contains(invoice.PaymentMethod))
            {
                ModelState.AddModelError(
                    "PaymentMethod",
                    "Phương thức thanh toán không hợp lệ"
                );
            }

            // KHÔNG CHO HỦY HÓA ĐƠN ĐÃ THU TIỀN
            decimal paidAmount = GetActualPaidAmount(existingInvoice);

            bool requestCancel = invoice.PaymentStatus == "Đã hủy";

            if (requestCancel && paidAmount > 0)
            {
                ModelState.AddModelError(
                    "PaymentStatus",
                    "Không thể hủy hóa đơn đã có thanh toán"
                );
            }

            // KHÔNG CHO THAY ĐỔI TỔNG TIỀN THẤP HƠN SỐ ĐÃ THU
            decimal newMedicineAmount = 0m;
            decimal newTotalAmount = 0m;

            if (appointment != null)
            {
                newMedicineAmount =
                    GetMedicineAmount(invoice.AppointmentID);

                newTotalAmount =
                    invoice.ConsultationFee + newMedicineAmount;

                if (newTotalAmount < paidAmount)
                {
                    ModelState.AddModelError(
                        "ConsultationFee",
                        "Tổng tiền hóa đơn không được thấp hơn số tiền đã thanh toán (" +
                        paidAmount.ToString("N0") + " VNĐ)"
                    );
                }
            }

            if (ModelState.IsValid)
            {
                string oldStatus = existingInvoice.PaymentStatus;
                string invoiceCode = existingInvoice.InvoiceCode;

                existingInvoice.InvoiceCode = invoice.InvoiceCode;
                existingInvoice.AppointmentID = invoice.AppointmentID;
                existingInvoice.InvoiceDate = invoice.InvoiceDate;
                existingInvoice.ConsultationFee = invoice.ConsultationFee;
                existingInvoice.MedicineAmount = newMedicineAmount;
                existingInvoice.TotalAmount = newTotalAmount;
                existingInvoice.Note = invoice.Note;

                // GIỮ NGUYÊN SỐ TIỀN ĐÃ THU
                existingInvoice.PaidAmount = paidAmount;

                if (requestCancel)
                {
                    existingInvoice.PaymentStatus = "Đã hủy";
                }
                else
                {
                    existingInvoice.PaymentStatus =
                        GetPaymentStatus(
                            existingInvoice.PaidAmount,
                            existingInvoice.TotalAmount
                        );
                }

                // Không xóa phương thức thanh toán đã ghi nhận
                if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod))
                {
                    existingInvoice.PaymentMethod = invoice.PaymentMethod;
                }

                db.SaveChanges();

                ActivityLogHelper.Write(
                    "Cập nhật",
                    "Hóa đơn",
                    "Cập nhật hóa đơn mã: " + invoiceCode
                );

                if (oldStatus != existingInvoice.PaymentStatus)
                {
                    ActivityLogHelper.Write(
                        "Thay đổi trạng thái",
                        "Hóa đơn",
                        "Hóa đơn " + invoiceCode +
                        " thay đổi trạng thái từ '" + oldStatus +
                        "' sang '" + existingInvoice.PaymentStatus + "'"
                    );
                }

                TempData["Success"] = "Cập nhật hóa đơn thành công!";

                return RedirectToAction("Index");
            }

            LoadDropdowns(
                invoice.AppointmentID,
                invoice.PaymentMethod,
                invoice.PaymentStatus
            );

            return View(invoice);
        }

        // GET: Invoice/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var invoice = db.Invoices
                .Include(i => i.Appointment.Patient)
                .Include(i => i.Appointment.Doctor)
                .FirstOrDefault(i => i.InvoiceID == id);

            if (invoice == null)
                return HttpNotFound();

            if (GetActualPaidAmount(invoice) > 0)
            {
                TempData["Error"] =
                    "Không thể xóa hóa đơn đã có thanh toán.";

                return RedirectToAction("Details", new { id = id });
            }

            return View(invoice);
        }

        // POST: Invoice/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var invoice = db.Invoices.Find(id);

            if (invoice == null)
                return HttpNotFound();

            if (GetActualPaidAmount(invoice) > 0)
            {
                TempData["Error"] =
                    "Không thể xóa hóa đơn đã có thanh toán.";

                return RedirectToAction("Details", new { id = id });
            }

            string invoiceCode = invoice.InvoiceCode;
            decimal totalAmount = invoice.TotalAmount;

            db.Invoices.Remove(invoice);
            db.SaveChanges();

            ActivityLogHelper.Write(
                "Xóa",
                "Hóa đơn",
                "Xóa hóa đơn mã: " + invoiceCode +
                ", tổng tiền: " + totalAmount.ToString("N0") + " VNĐ"
            );

            TempData["Success"] = "Xóa hóa đơn thành công!";

            return RedirectToAction("Index");
        }

        // GET: Invoice/Pay/5
        [HttpGet]
        public ActionResult Pay(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var invoice = db.Invoices.Find(id);

            if (invoice == null)
                return HttpNotFound();

            if (invoice.PaymentStatus == "Đã hủy")
            {
                TempData["Error"] =
                    "Hóa đơn đã hủy, không thể thanh toán.";

                return RedirectToAction("Details", new { id = id });
            }

            decimal paidAmount = GetActualPaidAmount(invoice);

            if (paidAmount >= invoice.TotalAmount)
            {
                TempData["Error"] =
                    "Hóa đơn đã được thanh toán đầy đủ.";

                return RedirectToAction("Details", new { id = id });
            }

            // Đồng bộ số tiền thực tế với dữ liệu hóa đơn cũ
            invoice.PaidAmount = paidAmount;

            return View(invoice);
        }

        // POST: Invoice/Pay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pay(int id, decimal amount, string paymentMethod)
        {
            var invoice = db.Invoices.Find(id);

            if (invoice == null)
                return HttpNotFound();

            if (invoice.PaymentStatus == "Đã hủy")
            {
                TempData["Error"] =
                    "Hóa đơn đã hủy, không thể thanh toán.";

                return RedirectToAction("Details", new { id = id });
            }

            if (amount <= 0)
            {
                TempData["Error"] =
                    "Số tiền thanh toán phải lớn hơn 0.";

                return RedirectToAction("Pay", new { id = id });
            }

            if (!paymentMethods.Contains(paymentMethod))
            {
                TempData["Error"] =
                    "Phương thức thanh toán không hợp lệ.";

                return RedirectToAction("Pay", new { id = id });
            }

            decimal paidAmount = GetActualPaidAmount(invoice);
            decimal remaining = invoice.TotalAmount - paidAmount;

            if (remaining <= 0)
            {
                TempData["Error"] =
                    "Hóa đơn đã được thanh toán đầy đủ.";

                return RedirectToAction("Details", new { id = id });
            }

            if (amount > remaining)
            {
                TempData["Error"] =
                    "Số tiền thanh toán vượt quá số tiền còn nợ.";

                return RedirectToAction("Pay", new { id = id });
            }

            decimal oldPaidAmount = paidAmount;

            invoice.PaidAmount = paidAmount + amount;
            invoice.PaymentMethod = paymentMethod;

            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.PaidAmount = invoice.TotalAmount;
                invoice.PaymentStatus = "Đã thanh toán";
            }
            else
            {
                invoice.PaymentStatus = "Thanh toán một phần";
            }

            db.SaveChanges();

            ActivityLogHelper.Write(
                "Thanh toán",
                "Hóa đơn",
                "Hóa đơn " + invoice.InvoiceCode +
                ": thanh toán thêm " + amount.ToString("N0") +
                " VNĐ. Đã trả: " + invoice.PaidAmount.ToString("N0") +
                " VNĐ. Trước đó đã trả: " + oldPaidAmount.ToString("N0") +
                " VNĐ."
            );

            TempData["Success"] =
                "Ghi nhận thanh toán thành công!";

            return RedirectToAction("Details", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
