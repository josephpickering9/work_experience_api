using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class MoveProjectImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate 'Image' to 'ProjectImages' as type Logo (assuming Logo = 0)
            migrationBuilder.Sql(@"
                INSERT INTO ""ProjectImage"" (""Image"", ""Type"", ""ProjectId"")
                SELECT ""Image"", 'Logo', ""Id""
                FROM ""Project""
                WHERE ""Image"" IS NOT NULL
            ");

            // Migrate 'BackgroundImage' to 'ProjectImages' as type Banner (assuming Banner = 1)
            migrationBuilder.Sql(@"
                INSERT INTO ""ProjectImage"" (""Image"", ""Type"", ""ProjectId"")
                SELECT ""BackgroundImage"", 'Banner', ""Id""
                FROM ""Project""
                WHERE ""BackgroundImage"" IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
