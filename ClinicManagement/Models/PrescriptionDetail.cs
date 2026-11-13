
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    public class PrescriptionDetail
    {
        [Key]
        public int PrescriptionDetailID { get; set; }

        [Required]
        public int PrescriptionID { get; set; }

        [ForeignKey("PrescriptionID")]
        public virtual Prescription Prescription { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thuốc")]
        public int MedicineID { get; set; }

        [ForeignKey("MedicineID")]
        public virtual Medicine Medicine { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập liều dùng")]
        [StringLength(200)]
        public string Dosage { get; set; }

        [StringLength(200)]
        public string Frequency { get; set; }

        [Range(1, 3650, ErrorMessage = "Số ngày sử dụng không hợp lệ")]
        public int? DurationDays { get; set; }

        [StringLength(500)]
        public string Instructions { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal")]
        public decimal LineTotal { get; set; }
    }
}
