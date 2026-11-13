namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateMedicalRecordTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MedicalRecords",
                c => new
                    {
                        RecordID = c.Int(nullable: false, identity: true),
                        RecordCode = c.String(nullable: false, maxLength: 20),
                        AppointmentID = c.Int(nullable: false),
                        RecordDate = c.DateTime(nullable: false),
                        Symptoms = c.String(maxLength: 1000),
                        Diagnosis = c.String(nullable: false, maxLength: 1000),
                        Treatment = c.String(maxLength: 2000),
                        DoctorAdvice = c.String(maxLength: 1000),
                        Note = c.String(maxLength: 1000),
                    })
                .PrimaryKey(t => t.RecordID)
                .ForeignKey("dbo.Appointments", t => t.AppointmentID, cascadeDelete: true)
                .Index(t => t.AppointmentID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MedicalRecords", "AppointmentID", "dbo.Appointments");
            DropIndex("dbo.MedicalRecords", new[] { "AppointmentID" });
            DropTable("dbo.MedicalRecords");
        }
    }
}
