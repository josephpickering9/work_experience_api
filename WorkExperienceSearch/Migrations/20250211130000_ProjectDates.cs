using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class ProjectDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Project",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Project",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Project"
                SET "StartDate" = make_date(
                        CASE
                            WHEN "Year" BETWEEN 1 AND 9999 THEN "Year"
                            ELSE 1
                        END,
                        1,
                        1
                    ),
                    "EndDate" = make_date(
                        CASE
                            WHEN "Year" BETWEEN 1 AND 9999 THEN "Year"
                            ELSE 1
                        END,
                        12,
                        31
                    )
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "Project",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Project");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Project",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "Project"
                SET "Year" = EXTRACT(YEAR FROM "StartDate")
                WHERE "StartDate" IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Project");
        }
    }
}
