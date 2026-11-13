
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
        [Column(TypeName = "varchar")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(255)]
        [Column(TypeName = "varchar")]
        public string PasswordHash { get; set; }

        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Column(TypeName = "varchar")]
        public string FullName { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [Column(TypeName = "varchar")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quyền.")]
        public int RoleID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }

        // Chỉ tài khoản Doctor mới cần liên kết DoctorID
        public int? DoctorID { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor Doctor { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}
