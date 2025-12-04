using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class UseGuidIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"ProjectRepository\" ADD COLUMN IF NOT EXISTS \"NewId\" uuid NOT NULL DEFAULT uuid_generate_v4();");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProjectRepository\" ADD COLUMN IF NOT EXISTS \"NewProjectId\" uuid NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE \"ProjectImage\" ADD COLUMN IF NOT EXISTS \"NewId\" uuid NOT NULL DEFAULT uuid_generate_v4();");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProjectImage\" ADD COLUMN IF NOT EXISTS \"NewProjectId\" uuid NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Project\" ADD COLUMN IF NOT EXISTS \"NewId\" uuid NOT NULL DEFAULT uuid_generate_v4();");
            migrationBuilder.Sql(
                "ALTER TABLE \"Project\" ADD COLUMN IF NOT EXISTS \"NewCompanyId\" uuid NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE \"ProjectTag\" ADD COLUMN IF NOT EXISTS \"NewTagsId\" uuid NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE \"ProjectTag\" ADD COLUMN IF NOT EXISTS \"NewProjectsId\" uuid NULL;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Company\" ADD COLUMN IF NOT EXISTS \"NewId\" uuid NOT NULL DEFAULT uuid_generate_v4();");

            migrationBuilder.Sql(
                "ALTER TABLE \"Tag\" ADD COLUMN IF NOT EXISTS \"NewId\" uuid NOT NULL DEFAULT uuid_generate_v4();");

            migrationBuilder.Sql("""
                UPDATE "Project" p
                SET "NewCompanyId" = c."NewId"
                FROM "Company" c
                WHERE p."CompanyId" = c."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "ProjectImage" pi
                SET "NewProjectId" = p."NewId"
                FROM "Project" p
                WHERE pi."ProjectId" = p."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "ProjectRepository" pr
                SET "NewProjectId" = p."NewId"
                FROM "Project" p
                WHERE pr."ProjectId" = p."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "ProjectTag" pt
                SET "NewProjectsId" = p."NewId",
                    "NewTagsId" = t."NewId"
                FROM "Project" p, "Tag" t
                WHERE pt."ProjectsId" = p."Id"
                  AND pt."TagsId" = t."Id";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Project_Company_CompanyId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectImage_Project_ProjectId",
                table: "ProjectImage");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectRepository_Project_ProjectId",
                table: "ProjectRepository");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTag_Project_ProjectsId",
                table: "ProjectTag");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTag_Tag_TagsId",
                table: "ProjectTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectTag",
                table: "ProjectTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectRepository",
                table: "ProjectRepository");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectImage",
                table: "ProjectImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Project",
                table: "Project");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tag",
                table: "Tag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Company",
                table: "Company");

            migrationBuilder.DropIndex(
                name: "IX_Project_CompanyId",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_ProjectRepository_ProjectId",
                table: "ProjectRepository");

            migrationBuilder.DropIndex(
                name: "IX_ProjectImage_ProjectId",
                table: "ProjectImage");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTag_TagsId",
                table: "ProjectTag");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ProjectRepository");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProjectRepository");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ProjectImage");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProjectImage");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "TagsId",
                table: "ProjectTag");

            migrationBuilder.DropColumn(
                name: "ProjectsId",
                table: "ProjectTag");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Tag");

            migrationBuilder.RenameColumn(
                name: "NewProjectId",
                table: "ProjectRepository",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "NewId",
                table: "ProjectRepository",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NewProjectId",
                table: "ProjectImage",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "NewId",
                table: "ProjectImage",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NewCompanyId",
                table: "Project",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "NewId",
                table: "Project",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NewTagsId",
                table: "ProjectTag",
                newName: "TagsId");

            migrationBuilder.RenameColumn(
                name: "NewProjectsId",
                table: "ProjectTag",
                newName: "ProjectsId");

            migrationBuilder.RenameColumn(
                name: "NewId",
                table: "Company",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NewId",
                table: "Tag",
                newName: "Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectRepository",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectImage",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectsId",
                table: "ProjectTag",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TagsId",
                table: "ProjectTag",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectTag",
                table: "ProjectTag",
                columns: new[] { "ProjectsId", "TagsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectRepository",
                table: "ProjectRepository",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectImage",
                table: "ProjectImage",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Project",
                table: "Project",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tag",
                table: "Tag",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Company",
                table: "Company",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Project_CompanyId",
                table: "Project",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRepository_ProjectId",
                table: "ProjectRepository",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImage_ProjectId",
                table: "ProjectImage",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTag_TagsId",
                table: "ProjectTag",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Company_CompanyId",
                table: "Project",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectImage_Project_ProjectId",
                table: "ProjectImage",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectRepository_Project_ProjectId",
                table: "ProjectRepository",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTag_Project_ProjectsId",
                table: "ProjectTag",
                column: "ProjectsId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTag_Tag_TagsId",
                table: "ProjectTag",
                column: "TagsId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Reverting from GUID IDs to integer IDs is not supported.");
        }
    }
}
