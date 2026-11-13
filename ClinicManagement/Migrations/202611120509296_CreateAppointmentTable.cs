namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateAppointmentTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Appointments",
                c => new
                    {
                        AppointmentID = c.Int(nullable: false, identity: true),
                        AppointmentCode = c.String(nullable: false, maxLength: 20),
                        PatientID = c.Int(nullable: false),
                        DoctorID = c.Int(nullable: false),
                        AppointmentDate = c.DateTime(nullable: false),
                        AppointmentTime = c.Time(nullable: false, precision: 7),
                        Reason = c.String(maxLength: 500),
                        Status = c.String(maxLength: 30),
                        Note = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.AppointmentID)
                .ForeignKey("dbo.Doctors", t => t.DoctorID)
                .ForeignKey("dbo.Patients", t => t.PatientID)
                .Index(t => t.PatientID)
                .Index(t => t.DoctorID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Appointments", "PatientID", "dbo.Patients");
            DropForeignKey("dbo.Appointments", "DoctorID", "dbo.Doctors");
            DropIndex("dbo.Appointments", new[] { "DoctorID" });
            DropIndex("dbo.Appointments", new[] { "PatientID" });
            DropTable("dbo.Appointments");
        }
    }
}
