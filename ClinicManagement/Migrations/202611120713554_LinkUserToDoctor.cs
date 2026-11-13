namespace ClinicManagement.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LinkUserToDoctor : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "DoctorID", c => c.Int());
            CreateIndex("dbo.Users", "DoctorID");
            AddForeignKey("dbo.Users", "DoctorID", "dbo.Doctors", "DoctorID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Users", "DoctorID", "dbo.Doctors");
            DropIndex("dbo.Users", new[] { "DoctorID" });
            DropColumn("dbo.Users", "DoctorID");
        }
    }
}
