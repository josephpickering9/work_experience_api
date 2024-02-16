using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class TagIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Colour",
                table: "Tag",
                newName: "Icon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "Tag",
                newName: "Colour");
        }
    }
}
