using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class ProjectShowMockup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowMockup",
                table: "Project",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowMockup",
                table: "Project");
        }
    }
}
