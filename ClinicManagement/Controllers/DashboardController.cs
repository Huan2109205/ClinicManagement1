
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using ClinicManagement.DAL;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager,Receptionist,Doctor")]
    public class DashboardController : Controller
    {
        private readonly ClinicDbContext db = new ClinicDbContext();

        public ActionResult Index()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // 1. TỔNG SỐ BỆNH NHÂN
            ViewBag.TotalPatients = db.Patients.Count();

            // 2. TỔNG SỐ BÁC SĨ ĐANG HOẠT ĐỘNG
            ViewBag.TotalDoctors = db.Doctors.Count(
                d => d.IsActive);

            // 3. TỔNG SỐ LỊCH KHÁM
            ViewBag.TotalAppointments = db.Appointments.Count();

            // 4. SỐ LỊCH KHÁM HÔM NAY
            ViewBag.TodayAppointments = db.Appointments.Count(
                a => a.AppointmentDate >= today &&
                     a.AppointmentDate < tomorrow);

            // 5. TỔNG SỐ HÓA ĐƠN
            ViewBag.TotalInvoices = db.Invoices.Count();

            // 6. SỐ HÓA ĐƠN ĐÃ HỦY
            ViewBag.CancelledInvoices = db.Invoices.Count(
                i => i.PaymentStatus == "Đã hủy");

            // 7. SỐ HÓA ĐƠN CHƯA THANH TOÁN
            ViewBag.UnpaidInvoices = db.Invoices.Count(
                i => i.PaymentStatus == "Chưa thanh toán");

            // 8. SỐ HÓA ĐƠN ĐÃ THANH TOÁN
            ViewBag.PaidInvoices = db.Invoices.Count(
                i => i.PaymentStatus == "Đã thanh toán");

            // 9. SỐ HÓA ĐƠN THANH TOÁN MỘT PHẦN
            ViewBag.PartialInvoices = db.Invoices.Count(
                i => i.PaymentStatus == "Thanh toán một phần");

            // 10. TỔNG GIÁ TRỊ HÓA ĐƠN
            ViewBag.TotalInvoiceAmount = db.Invoices
                .Where(i => i.PaymentStatus != "Đã hủy")
                .Select(i => (decimal?)i.TotalAmount)
                .Sum() ?? 0m;

            // 11. TỔNG DOANH THU THỰC THU
            // Hóa đơn cũ đã thanh toán nhưng PaidAmount <= 0
            // được tính là đã thu đủ TotalAmount.
            // Các trường hợp khác giới hạn số tiền thu trong khoảng 0..TotalAmount.
            ViewBag.TotalRevenue = db.Invoices
                .Where(i => i.PaymentStatus != "Đã hủy")
                .Select(i => (decimal?)(
                    i.PaymentStatus == "Đã thanh toán" &&
                    i.PaidAmount <= 0
                        ? i.TotalAmount
                        : i.PaidAmount <= 0
                            ? 0m
                            : i.PaidAmount >= i.TotalAmount
                                ? i.TotalAmount
                                : i.PaidAmount))
                .Sum() ?? 0m;

            // 12. TỔNG CÔNG NỢ CÒN PHẢI THU
            ViewBag.TotalRemaining = db.Invoices
                .Where(i => i.PaymentStatus != "Đã hủy")
                .Select(i => (decimal?)(
                    i.PaymentStatus == "Đã thanh toán" &&
                    i.PaidAmount <= 0
                        ? 0m
                        : i.PaidAmount <= 0
                            ? (i.TotalAmount > 0 ? i.TotalAmount : 0m)
                            : i.PaidAmount >= i.TotalAmount
                                ? 0m
                                : i.TotalAmount - i.PaidAmount))
                .Sum() ?? 0m;

            // 13. DOANH THU THEO 12 THÁNG TRONG NĂM HIỆN TẠI
            int currentYear = today.Year;

            DateTime firstDayOfYear =
                new DateTime(currentYear, 1, 1);

            DateTime firstDayOfNextYear =
                firstDayOfYear.AddYears(1);

            var monthlyInvoices = db.Invoices
                .Where(i =>
                    i.PaymentStatus != "Đã hủy" &&
                    i.InvoiceDate >= firstDayOfYear &&
                    i.InvoiceDate < firstDayOfNextYear)
                .GroupBy(i => i.InvoiceDate.Month)
                .Select(g => new
                {
                    Month = g.Key,

                    Revenue = g.Sum(i =>
                        i.PaymentStatus == "Đã thanh toán" &&
                        i.PaidAmount <= 0
                            ? i.TotalAmount
                            : i.PaidAmount <= 0
                                ? 0m
                                : i.PaidAmount >= i.TotalAmount
                                    ? i.TotalAmount
                                    : i.PaidAmount),

                    Debt = g.Sum(i =>
                        i.PaymentStatus == "Đã thanh toán" &&
                        i.PaidAmount <= 0
                            ? 0m
                            : i.PaidAmount <= 0
                                ? (i.TotalAmount > 0 ? i.TotalAmount : 0m)
                                : i.PaidAmount >= i.TotalAmount
                                    ? 0m
                                    : i.TotalAmount - i.PaidAmount)
                })
                .ToList();

            decimal[] revenueData = new decimal[12];
            decimal[] debtData = new decimal[12];

            foreach (var item in monthlyInvoices)
            {
                revenueData[item.Month - 1] = item.Revenue;
                debtData[item.Month - 1] = item.Debt;
            }

            ViewBag.RevenueLabels = new[]
            {
                "Tháng 1", "Tháng 2", "Tháng 3",
                "Tháng 4", "Tháng 5", "Tháng 6",
                "Tháng 7", "Tháng 8", "Tháng 9",
                "Tháng 10", "Tháng 11", "Tháng 12"
            };

            ViewBag.RevenueData = revenueData;
            ViewBag.DebtData = debtData;
            ViewBag.SelectedYear = currentYear;

            // 14. 8 LỊCH KHÁM GẦN ĐÂY
            ViewBag.RecentAppointments = db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .Take(8)
                .ToList();

            return View();
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
