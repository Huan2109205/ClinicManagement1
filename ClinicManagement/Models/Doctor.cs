using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã bác sĩ")]
        [StringLength(20)]
        public string DoctorCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; }

        public string Specialty { get; set; }

        [StringLength(15)]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string Address { get; set; }

        public bool IsActive { get; set; }
    
        public int? SpecialtyID { get; set; }

        [ForeignKey("SpecialtyID")]
        public virtual Specialty SpecialtyNavigation { get; set; }
    }
}
