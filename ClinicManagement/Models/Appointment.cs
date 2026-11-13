using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã lịch khám")]
        [StringLength(20)]
        public string AppointmentCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bệnh nhân")]
        public int PatientID { get; set; }

        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bác sĩ")]
        public int DoctorID { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor Doctor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày khám")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giờ khám")]
        public TimeSpan AppointmentTime { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [StringLength(30)]
        public string Status { get; set; }

        [StringLength(500)]
        public string Note { get; set; }
    }
}