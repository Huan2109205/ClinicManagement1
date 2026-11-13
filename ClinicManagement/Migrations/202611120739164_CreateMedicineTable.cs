namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateMedicineTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Medicines",
                c => new
                    {
                        MedicineID = c.Int(nullable: false, identity: true),
                        MedicineCode = c.String(nullable: false, maxLength: 20),
                        MedicineName = c.String(nullable: false, maxLength: 150),
                        ActiveIngredient = c.String(maxLength: 100),
                        Unit = c.String(maxLength: 50),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Description = c.String(maxLength: 500),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.MedicineID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Medicines");
        }
    }
}
