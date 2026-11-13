using System.Data.Entity;
using ClinicManagement.Models;

namespace ClinicManagement.DAL
{
    public class ClinicDbContext : DbContext
    {
        public ClinicDbContext()
            : base("name=ClinicConnection")
        {
        }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Doctor> Doctors { get; set; }

        public DbSet<Specialty> Specialties { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionDetail> PrescriptionDetails { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
    }
}