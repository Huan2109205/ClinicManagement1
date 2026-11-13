
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Models
{
    public class DoctorProfileViewModel
    {
        public string DoctorCode { get; set; }

        public string FullName { get; set; }

        public string SpecialtyName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(255)]
        public string Address { get; set; }
    }
}
