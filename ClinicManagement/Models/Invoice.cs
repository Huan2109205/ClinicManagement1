
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã hóa đơn")]
        [StringLength(20)]
        public string InvoiceCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lịch khám")]
        public int AppointmentID { get; set; }

        [ForeignKey("AppointmentID")]
        public virtual Appointment Appointment { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        // TIỀN KHÁM
        [Required(ErrorMessage = "Vui lòng nhập tiền khám")]
        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Tiền khám không hợp lệ")]
        [Column(TypeName = "decimal")]
        public decimal ConsultationFee { get; set; }

        // TIỀN THUỐC
        [Required]
        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Tiền thuốc không hợp lệ")]
        [Column(TypeName = "decimal")]
        public decimal MedicineAmount { get; set; }

        // TỔNG TIỀN
        [Required]
        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Tổng tiền không hợp lệ")]
        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        // SỐ TIỀN ĐÃ THANH TOÁN
        [Required]
        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Số tiền đã thanh toán không hợp lệ")]
        [Column(TypeName = "decimal")]
        public decimal PaidAmount { get; set; }

        // SỐ TIỀN CÒN PHẢI THANH TOÁN
        // Không lưu xuống Database, tự động tính
        [NotMapped]
        public decimal RemainingAmount
        {
            get
            {
                decimal remaining = TotalAmount - PaidAmount;
                return remaining > 0 ? remaining : 0;
            }
        }

        // PHƯƠNG THỨC THANH TOÁN
        [StringLength(30)]
        public string PaymentMethod { get; set; }

        // TRẠNG THÁI THANH TOÁN
        [StringLength(30)]
        public string PaymentStatus { get; set; }

        // GHI CHÚ
        [StringLength(500)]
        public string Note { get; set; }
    }
}
