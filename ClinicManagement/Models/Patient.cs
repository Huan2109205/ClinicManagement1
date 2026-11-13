
using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Models
{
    public class Patient
    {
        public int PatientID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã bệnh nhân")]
        [StringLength(20)]
        public string PatientCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        public string Address { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }
}
