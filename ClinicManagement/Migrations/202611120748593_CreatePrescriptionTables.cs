namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreatePrescriptionTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PrescriptionDetails",
                c => new
                    {
                        PrescriptionDetailID = c.Int(nullable: false, identity: true),
                        PrescriptionID = c.Int(nullable: false),
                        MedicineID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        Dosage = c.String(nullable: false, maxLength: 200),
                        Frequency = c.String(maxLength: 200),
                        DurationDays = c.Int(),
                        Instructions = c.String(maxLength: 500),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        LineTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.PrescriptionDetailID)
                .ForeignKey("dbo.Medicines", t => t.MedicineID, cascadeDelete: true)
                .ForeignKey("dbo.Prescriptions", t => t.PrescriptionID, cascadeDelete: true)
                .Index(t => t.PrescriptionID)
                .Index(t => t.MedicineID);
            
            CreateTable(
                "dbo.Prescriptions",
                c => new
                    {
                        PrescriptionID = c.Int(nullable: false, identity: true),
                        PrescriptionCode = c.String(nullable: false, maxLength: 20),
                        RecordID = c.Int(nullable: false),
                        PrescriptionDate = c.DateTime(nullable: false),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Note = c.String(maxLength: 1000),
                    })
                .PrimaryKey(t => t.PrescriptionID)
                .ForeignKey("dbo.MedicalRecords", t => t.RecordID, cascadeDelete: true)
                .Index(t => t.RecordID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Prescriptions", "RecordID", "dbo.MedicalRecords");
            DropForeignKey("dbo.PrescriptionDetails", "PrescriptionID", "dbo.Prescriptions");
            DropForeignKey("dbo.PrescriptionDetails", "MedicineID", "dbo.Medicines");
            DropIndex("dbo.Prescriptions", new[] { "RecordID" });
            DropIndex("dbo.PrescriptionDetails", new[] { "MedicineID" });
            DropIndex("dbo.PrescriptionDetails", new[] { "PrescriptionID" });
            DropTable("dbo.Prescriptions");
            DropTable("dbo.PrescriptionDetails");
        }
    }
}
