
using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityLogID { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; }

        [Required]
        [StringLength(100)]
        public string Module { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        [StringLength(50)]
        public string IPAddress { get; set; }
    }
}
