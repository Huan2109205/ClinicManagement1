namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPaidAmountToInvoice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Invoices", "PaidAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Invoices", "PaidAmount");
        }
    }
}
