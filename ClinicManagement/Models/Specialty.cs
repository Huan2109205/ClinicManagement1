using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Models
{
    public class Specialty
    {
        [Key]
        public int SpecialtyID { get; set; }

        [Required]
        [StringLength(20)]
        public string SpecialtyCode { get; set; }

        [Required]
        [StringLength(100)]
        public string SpecialtyName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; }
    }
}