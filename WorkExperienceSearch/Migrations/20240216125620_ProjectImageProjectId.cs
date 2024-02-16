using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class ProjectImageProjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tag_Slug",
                table: "Tag",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_Slug",
                table: "Project",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Company_Slug",
                table: "Company",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tag_Slug",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Project_Slug",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Company_Slug",
                table: "Company");
        }
    }
}
