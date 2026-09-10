using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class AddProductSourceSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OSIS");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SourceSystem_Type_IsActive",
                table: "Products",
                columns: new[] { "SourceSystem", "Type", "IsActive" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SourceSystem_Type_IsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "Products");
        }
    }
}
