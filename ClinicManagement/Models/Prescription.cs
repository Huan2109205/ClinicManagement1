
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã đơn thuốc")]
        [StringLength(20)]
        public string PrescriptionCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hồ sơ khám bệnh")]
        public int RecordID { get; set; }

        [ForeignKey("RecordID")]
        public virtual MedicalRecord MedicalRecord { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PrescriptionDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        [StringLength(1000)]
        public string Note { get; set; }

        public virtual ICollection<PrescriptionDetail> Details { get; set; }
    }
}
