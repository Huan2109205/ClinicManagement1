namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddInvoiceFeeBreakdown : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Invoices", "ConsultationFee", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Invoices", "MedicineAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Invoices", "MedicineAmount");
            DropColumn("dbo.Invoices", "ConsultationFee");
        }
    }
}
