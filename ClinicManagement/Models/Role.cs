
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên quyền")]
        [StringLength(50)]
        [Column(TypeName = "nvarchar")]
        public string RoleName { get; set; }

        [StringLength(255)]
        [Column(TypeName = "nvarchar")]
        public string Description { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
