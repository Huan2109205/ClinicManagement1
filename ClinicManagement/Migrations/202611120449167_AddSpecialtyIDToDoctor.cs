namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSpecialtyIDToDoctor : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Doctors", "SpecialtyID", c => c.Int());
            CreateIndex("dbo.Doctors", "SpecialtyID");
            AddForeignKey("dbo.Doctors", "SpecialtyID", "dbo.Specialties", "SpecialtyID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Doctors", "SpecialtyID", "dbo.Specialties");
            DropIndex("dbo.Doctors", new[] { "SpecialtyID" });
            DropColumn("dbo.Doctors", "SpecialtyID");
        }
    }
}
