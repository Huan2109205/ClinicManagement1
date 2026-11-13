namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateInvoiceTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Invoices",
                c => new
                    {
                        InvoiceID = c.Int(nullable: false, identity: true),
                        InvoiceCode = c.String(nullable: false, maxLength: 20),
                        AppointmentID = c.Int(nullable: false),
                        InvoiceDate = c.DateTime(nullable: false),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PaymentMethod = c.String(maxLength: 30),
                        PaymentStatus = c.String(maxLength: 30),
                        Note = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.InvoiceID)
                .ForeignKey("dbo.Appointments", t => t.AppointmentID)
                .Index(t => t.AppointmentID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Invoices", "AppointmentID", "dbo.Appointments");
            DropIndex("dbo.Invoices", new[] { "AppointmentID" });
            DropTable("dbo.Invoices");
        }
    }
}
