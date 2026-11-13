
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    public class Medicine
    {
        [Key]
        public int MedicineID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã thuốc")]
        [StringLength(20)]
        public string MedicineCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thuốc")]
        [StringLength(150)]
        public string MedicineName { get; set; }

        [StringLength(100)]
        public string ActiveIngredient { get; set; }

        [StringLength(50)]
        public string Unit { get; set; }

        [Range(0, 999999999)]
        [Column(TypeName = "decimal")]
        public decimal UnitPrice { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; }
    }
}
