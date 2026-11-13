
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    public class MedicalRecord
    {
        [Key]
        public int RecordID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã hồ sơ")]
        [StringLength(20)]
        public string RecordCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lịch khám")]
        public int AppointmentID { get; set; }

        [ForeignKey("AppointmentID")]
        public virtual Appointment Appointment { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RecordDate { get; set; }

        [StringLength(1000)]
        public string Symptoms { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chẩn đoán")]
        [StringLength(1000)]
        public string Diagnosis { get; set; }

        [StringLength(2000)]
        public string Treatment { get; set; }

        [StringLength(1000)]
        public string DoctorAdvice { get; set; }

        [StringLength(1000)]
        public string Note { get; set; }
    }
}
