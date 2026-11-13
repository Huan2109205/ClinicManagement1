
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Data.Entity;
using ClinicManagement.DAL;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace ClinicManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class StatisticsController : Controller
    {
        private readonly ClinicDbContext db = new ClinicDbContext();

        // LẤY SỐ TIỀN ĐÃ THU THỰC TẾ
        // Xử lý cả hóa đơn cũ có trạng thái Đã thanh toán
        // nhưng PaidAmount vẫn bằng 0.
        private decimal GetActualPaidAmount(ClinicManagement.Models.Invoice invoice)
        {
            if (invoice == null || invoice.PaymentStatus == "Đã hủy")
                return 0m;

            if (invoice.PaymentStatus == "Đã thanh toán" &&
                invoice.PaidAmount <= 0)
            {
                return Math.Max(0m, invoice.TotalAmount);
            }

            return Math.Max(
                0m,
                Math.Min(invoice.PaidAmount, invoice.TotalAmount)
            );
        }

        // TÍNH CÔNG NỢ
        private decimal GetRemainingAmount(ClinicManagement.Models.Invoice invoice)
        {
            if (invoice == null || invoice.PaymentStatus == "Đã hủy")
                return 0m;

            return Math.Max(
                0m,
                invoice.TotalAmount - GetActualPaidAmount(invoice)
            );
        }

        // TRANG THỐNG KÊ
        public ActionResult Index(int? year)
        {
            int selectedYear = year ?? DateTime.Today.Year;

            if (selectedYear < 2000 || selectedYear > 2100)
                selectedYear = DateTime.Today.Year;

            DateTime startDate = new DateTime(selectedYear, 1, 1);
            DateTime endDate = startDate.AddYears(1);

            // LẤY HÓA ĐƠN TRONG NĂM
            var invoices = db.Invoices
                .Where(i => i.InvoiceDate >= startDate &&
                            i.InvoiceDate < endDate)
                .ToList();

            // LOẠI BỎ HÓA ĐƠN ĐÃ HỦY
            var validInvoices = invoices
                .Where(i => i.PaymentStatus != "Đã hủy")
                .ToList();

            // TỔNG GIÁ TRỊ HÓA ĐƠN
            decimal totalInvoiceAmount = validInvoices
                .Sum(i => i.TotalAmount);

            // TỔNG TIỀN ĐÃ THU THỰC TẾ
            decimal totalRevenue = validInvoices
                .Sum(i => GetActualPaidAmount(i));

            // TỔNG CÔNG NỢ
            decimal totalDebt = validInvoices
                .Sum(i => GetRemainingAmount(i));

            // SỐ HÓA ĐƠN ĐÃ CÓ THANH TOÁN
            int collectedInvoiceCount = validInvoices.Count(i =>
                GetActualPaidAmount(i) > 0);

            // DOANH THU TRUNG BÌNH TRÊN HÓA ĐƠN ĐÃ THU TIỀN
            decimal averageRevenue = collectedInvoiceCount > 0
                ? totalRevenue / collectedInvoiceCount
                : 0m;

            // DOANH THU TIỀN KHÁM VÀ TIỀN THUỐC
            // Phân bổ số tiền đã thu theo tỷ trọng tiền khám/tiền thuốc.
            decimal consultationRevenue = 0m;
            decimal medicineRevenue = 0m;

            foreach (var invoice in validInvoices)
            {
                decimal paid = GetActualPaidAmount(invoice);

                if (invoice.TotalAmount <= 0 || paid <= 0)
                    continue;

                decimal consultationRatio =
                    invoice.ConsultationFee / invoice.TotalAmount;

                decimal medicineRatio =
                    invoice.MedicineAmount / invoice.TotalAmount;

                consultationRevenue += paid * consultationRatio;
                medicineRevenue += paid * medicineRatio;
            }

            // DOANH THU HÔM NAY
            decimal todayRevenue = validInvoices
                .Where(i => i.InvoiceDate.Date == DateTime.Today)
                .Sum(i => GetActualPaidAmount(i));

            // DOANH THU THÁNG HIỆN TẠI
            decimal currentMonthRevenue = validInvoices
                .Where(i =>
                    i.InvoiceDate.Year == DateTime.Today.Year &&
                    i.InvoiceDate.Month == DateTime.Today.Month)
                .Sum(i => GetActualPaidAmount(i));

            // DOANH THU THEO THÁNG
            var monthlyRevenue = new decimal[12];
            var monthlyConsultation = new decimal[12];
            var monthlyMedicine = new decimal[12];
            var monthlyDebt = new decimal[12];

            foreach (var invoice in validInvoices)
            {
                int index = invoice.InvoiceDate.Month - 1;

                decimal paid = GetActualPaidAmount(invoice);
                decimal debt = GetRemainingAmount(invoice);

                monthlyRevenue[index] += paid;
                monthlyDebt[index] += debt;

                if (invoice.TotalAmount > 0 && paid > 0)
                {
                    monthlyConsultation[index] +=
                        paid * invoice.ConsultationFee /
                        invoice.TotalAmount;

                    monthlyMedicine[index] +=
                        paid * invoice.MedicineAmount /
                        invoice.TotalAmount;
                }
            }

            // THỐNG KÊ TRẠNG THÁI HÓA ĐƠN
            int totalInvoices = invoices.Count;

            int paidCount = invoices.Count(i =>
                i.PaymentStatus == "Đã thanh toán");

            int unpaidCount = invoices.Count(i =>
                i.PaymentStatus == "Chưa thanh toán");

            int partialCount = invoices.Count(i =>
                i.PaymentStatus == "Thanh toán một phần");

            int cancelledCount = invoices.Count(i =>
                i.PaymentStatus == "Đã hủy");

            // THỐNG KÊ LỊCH KHÁM
            var appointments = db.Appointments.ToList();

            var appointmentGroups = appointments
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Status)
                    ? "Chưa xác định"
                    : a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // NHÃN BIỂU ĐỒ
            var monthLabels = new[]
            {
                "Tháng 1", "Tháng 2", "Tháng 3",
                "Tháng 4", "Tháng 5", "Tháng 6",
                "Tháng 7", "Tháng 8", "Tháng 9",
                "Tháng 10", "Tháng 11", "Tháng 12"
            };

            var serializer = new JavaScriptSerializer();

            // DỮ LIỆU BIỂU ĐỒ DOANH THU
            ViewBag.MonthLabelsJson =
                serializer.Serialize(monthLabels);

            ViewBag.MonthRevenueJson =
                serializer.Serialize(monthlyRevenue);

            ViewBag.MonthConsultationJson =
                serializer.Serialize(monthlyConsultation);

            ViewBag.MonthMedicineJson =
                serializer.Serialize(monthlyMedicine);

            ViewBag.MonthDebtJson =
                serializer.Serialize(monthlyDebt);

            // DỮ LIỆU BIỂU ĐỒ LỊCH KHÁM
            ViewBag.AppointmentStatusJson =
                serializer.Serialize(
                    appointmentGroups.Select(x => x.Status).ToArray());

            ViewBag.AppointmentCountJson =
                serializer.Serialize(
                    appointmentGroups.Select(x => x.Count).ToArray());

            // CÁC SỐ LIỆU TỔNG QUAN
            ViewBag.SelectedYear = selectedYear;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalInvoiceAmount = totalInvoiceAmount;
            ViewBag.TotalDebt = totalDebt;

            ViewBag.ConsultationRevenue = consultationRevenue;
            ViewBag.MedicineRevenue = medicineRevenue;
            ViewBag.AverageRevenue = averageRevenue;

            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.CurrentMonthRevenue = currentMonthRevenue;

            ViewBag.TotalInvoices = totalInvoices;
            ViewBag.PaidInvoices = paidCount;
            ViewBag.UnpaidInvoices = unpaidCount;
            ViewBag.PartialInvoices = partialCount;
            ViewBag.CancelledInvoices = cancelledCount;
            ViewBag.CollectedInvoiceCount = collectedInvoiceCount;

            ViewBag.TotalAppointments = appointments.Count;

            ViewBag.TodayAppointments = appointments.Count(a =>
                a.AppointmentDate.Date == DateTime.Today);

            return View();
        }

        // XUẤT BÁO CÁO DOANH THU RA EXCEL
        public ActionResult ExportExcel(int? year)
        {
            int selectedYear = year ?? DateTime.Today.Year;

            if (selectedYear < 2000 || selectedYear > 2100)
                selectedYear = DateTime.Today.Year;

            DateTime startDate = new DateTime(selectedYear, 1, 1);
            DateTime endDate = startDate.AddYears(1);

            var invoices = db.Invoices
                .Include("Appointment.Patient")
                .Include("Appointment.Doctor")
                .Where(i => i.InvoiceDate >= startDate &&
                            i.InvoiceDate < endDate)
                .OrderBy(i => i.InvoiceDate)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(
                    "BaoCaoDoanhThu");

                // TIÊU ĐỀ
                worksheet.Cells["A1:K1"].Merge = true;

                worksheet.Cells["A1"].Value =
                    "BÁO CÁO DOANH THU PHÒNG KHÁM NĂM " +
                    selectedYear;

                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;

                worksheet.Cells["A1"].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;

                // TIÊU ĐỀ CỘT
                string[] headers =
                {
                    "STT",
                    "Mã hóa đơn",
                    "Bệnh nhân",
                    "Bác sĩ",
                    "Ngày lập",
                    "Tiền khám",
                    "Tiền thuốc",
                    "Tổng tiền",
                    "Đã thu",
                    "Còn nợ",
                    "Trạng thái"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[3, i + 1].Value = headers[i];
                }

                using (var headerRange = worksheet.Cells["A3:K3"])
                {
                    headerRange.Style.Font.Bold = true;

                    headerRange.Style.Fill.PatternType =
                        ExcelFillStyle.Solid;

                    headerRange.Style.Fill.BackgroundColor.SetColor(
                        Color.LightBlue);

                    headerRange.Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                }

                int row = 4;
                int stt = 1;

                decimal excelTotalAmount = 0m;
                decimal excelPaidAmount = 0m;
                decimal excelDebtAmount = 0m;

                foreach (var invoice in invoices)
                {
                    decimal paid = GetActualPaidAmount(invoice);
                    decimal debt = GetRemainingAmount(invoice);

                    worksheet.Cells[row, 1].Value = stt++;

                    worksheet.Cells[row, 2].Value =
                        invoice.InvoiceCode;

                    worksheet.Cells[row, 3].Value =
                        invoice.Appointment?.Patient?.FullName;

                    worksheet.Cells[row, 4].Value =
                        invoice.Appointment?.Doctor?.FullName;

                    worksheet.Cells[row, 5].Value =
                        invoice.InvoiceDate;

                    worksheet.Cells[row, 5].Style.Numberformat.Format =
                        "dd/MM/yyyy";

                    worksheet.Cells[row, 6].Value =
                        invoice.ConsultationFee;

                    worksheet.Cells[row, 7].Value =
                        invoice.MedicineAmount;

                    worksheet.Cells[row, 8].Value =
                        invoice.TotalAmount;

                    worksheet.Cells[row, 9].Value = paid;

                    worksheet.Cells[row, 10].Value = debt;

                    worksheet.Cells[row, 11].Value =
                        invoice.PaymentStatus;

                    // KHÔNG TÍNH HÓA ĐƠN ĐÃ HỦY VÀO TỔNG
                    if (invoice.PaymentStatus != "Đã hủy")
                    {
                        excelTotalAmount += invoice.TotalAmount;
                        excelPaidAmount += paid;
                        excelDebtAmount += debt;
                    }

                    row++;
                }

                // DÒNG TỔNG KẾT
                int summaryRow = row + 1;

                worksheet.Cells[summaryRow, 7].Value =
                    "TỔNG CỘNG";

                worksheet.Cells[summaryRow, 7].Style.Font.Bold = true;

                worksheet.Cells[summaryRow, 8].Value =
                    excelTotalAmount;

                worksheet.Cells[summaryRow, 9].Value =
                    excelPaidAmount;

                worksheet.Cells[summaryRow, 10].Value =
                    excelDebtAmount;

                using (var summaryRange = worksheet.Cells[
                    summaryRow, 7, summaryRow, 10])
                {
                    summaryRange.Style.Font.Bold = true;

                    summaryRange.Style.Fill.PatternType =
                        ExcelFillStyle.Solid;

                    summaryRange.Style.Fill.BackgroundColor.SetColor(
                        Color.LightGreen);
                }

                // ĐỊNH DẠNG TIỀN
                worksheet.Cells[4, 6, summaryRow, 10]
                    .Style.Numberformat.Format = "#,##0";

                // KẺ BẢNG
                if (row > 4)
                {
                    using (var tableRange = worksheet.Cells[
                        3, 1, row - 1, 11])
                    {
                        tableRange.Style.Border.Top.Style =
                            ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Bottom.Style =
                            ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Left.Style =
                            ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Right.Style =
                            ExcelBorderStyle.Thin;
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address]
                    .AutoFitColumns();

                byte[] fileContents = package.GetAsByteArray();

                return File(
                    fileContents,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "BaoCaoDoanhThu_" + selectedYear + ".xlsx"
                );
            }
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
