using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Work_Experience_Search.Migrations
{
    /// <inheritdoc />
    public partial class ReorderIdColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

            // Drop foreign keys to allow table recreation
            migrationBuilder.Sql("""
                ALTER TABLE "ProjectTag" DROP CONSTRAINT IF EXISTS "FK_ProjectTag_Project_ProjectsId";
                ALTER TABLE "ProjectTag" DROP CONSTRAINT IF EXISTS "FK_ProjectTag_Tag_TagsId";
                ALTER TABLE "ProjectImage" DROP CONSTRAINT IF EXISTS "FK_ProjectImage_Project_ProjectId";
                ALTER TABLE "ProjectRepository" DROP CONSTRAINT IF EXISTS "FK_ProjectRepository_Project_ProjectId";
                ALTER TABLE "Project" DROP CONSTRAINT IF EXISTS "FK_Project_Company_CompanyId";
                """);

            // Company
            migrationBuilder.Sql("""
                ALTER TABLE "Company" DROP CONSTRAINT IF EXISTS "PK_Company";
                DROP INDEX IF EXISTS "IX_Company_Slug";
                ALTER TABLE "Company" RENAME TO "Company_old";
                CREATE TABLE "Company"
                (
                    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
                    "Name" text NOT NULL,
                    "Description" text NOT NULL,
                    "Website" text NULL,
                    "Logo" text NULL,
                    "Slug" text NOT NULL,
                    CONSTRAINT "PK_Company" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX "IX_Company_Slug" ON "Company" ("Slug");
                INSERT INTO "Company" ("Id", "Name", "Description", "Website", "Logo", "Slug")
                SELECT "Id", "Name", "Description", "Website", "Logo", "Slug" FROM "Company_old";
                DROP TABLE "Company_old";
            """);

            // Tag
            migrationBuilder.Sql("""
                ALTER TABLE "Tag" DROP CONSTRAINT IF EXISTS "PK_Tag";
                DROP INDEX IF EXISTS "IX_Tag_Slug";
                ALTER TABLE "Tag" RENAME TO "Tag_old";
                CREATE TABLE "Tag"
                (
                    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
                    "Title" text NOT NULL,
                    "Type" text NOT NULL,
                    "Icon" text NULL,
                    "CustomColour" text NULL,
                    "Slug" text NOT NULL,
                    CONSTRAINT "PK_Tag" PRIMARY KEY ("Id")
                );
                CREATE UNIQUE INDEX "IX_Tag_Slug" ON "Tag" ("Slug");
                INSERT INTO "Tag" ("Id", "Title", "Type", "Icon", "CustomColour", "Slug")
                SELECT "Id", "Title", "Type", "Icon", "CustomColour", "Slug" FROM "Tag_old";
                DROP TABLE "Tag_old";
            """);

            // Project
            migrationBuilder.Sql("""
                ALTER TABLE "Project" DROP CONSTRAINT IF EXISTS "PK_Project";
                DROP INDEX IF EXISTS "IX_Project_Slug";
                DROP INDEX IF EXISTS "IX_Project_CompanyId";
                ALTER TABLE "Project" RENAME TO "Project_old";
                CREATE TABLE "Project"
                (
                    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
                    "Title" text NOT NULL,
                    "ShortDescription" text NOT NULL,
                    "Description" text NOT NULL,
                    "CompanyId" uuid NULL,
                    "Year" integer NOT NULL,
                    "Website" text NULL,
                    "ShowMockup" boolean NOT NULL,
                    "Slug" text NOT NULL,
                    CONSTRAINT "PK_Project" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Project_Company_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Company" ("Id")
                );
                CREATE UNIQUE INDEX "IX_Project_Slug" ON "Project" ("Slug");
                CREATE INDEX "IX_Project_CompanyId" ON "Project" ("CompanyId");
                INSERT INTO "Project" ("Id", "Title", "ShortDescription", "Description", "CompanyId", "Year", "Website", "ShowMockup", "Slug")
                SELECT "Id", "Title", "ShortDescription", "Description", "CompanyId", "Year", "Website", "ShowMockup", "Slug" FROM "Project_old";
                DROP TABLE "Project_old";
            """);

            // ProjectImage
            migrationBuilder.Sql("""
                ALTER TABLE "ProjectImage" DROP CONSTRAINT IF EXISTS "PK_ProjectImage";
                DROP INDEX IF EXISTS "IX_ProjectImage_ProjectId";
                ALTER TABLE "ProjectImage" RENAME TO "ProjectImage_old";
                CREATE TABLE "ProjectImage"
                (
                    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
                    "Image" text NOT NULL,
                    "Type" text NOT NULL,
                    "Order" integer NULL,
                    "IsOptimised" boolean NOT NULL,
                    "ProjectId" uuid NOT NULL,
                    CONSTRAINT "PK_ProjectImage" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ProjectImage_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX "IX_ProjectImage_ProjectId" ON "ProjectImage" ("ProjectId");
                INSERT INTO "ProjectImage" ("Id", "Image", "Type", "Order", "IsOptimised", "ProjectId")
                SELECT "Id", "Image", "Type", "Order", "IsOptimised", "ProjectId" FROM "ProjectImage_old";
                DROP TABLE "ProjectImage_old";
            """);

            // ProjectRepository
            migrationBuilder.Sql("""
                ALTER TABLE "ProjectRepository" DROP CONSTRAINT IF EXISTS "PK_ProjectRepository";
                DROP INDEX IF EXISTS "IX_ProjectRepository_ProjectId";
                ALTER TABLE "ProjectRepository" RENAME TO "ProjectRepository_old";
                CREATE TABLE "ProjectRepository"
                (
                    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
                    "Title" text NOT NULL,
                    "Url" text NOT NULL,
                    "Order" integer NULL,
                    "ProjectId" uuid NOT NULL,
                    CONSTRAINT "PK_ProjectRepository" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ProjectRepository_Project_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Project" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX "IX_ProjectRepository_ProjectId" ON "ProjectRepository" ("ProjectId");
                INSERT INTO "ProjectRepository" ("Id", "Title", "Url", "Order", "ProjectId")
                SELECT "Id", "Title", "Url", "Order", "ProjectId" FROM "ProjectRepository_old";
                DROP TABLE "ProjectRepository_old";
            """);

            // ProjectTag
            migrationBuilder.Sql("""
                ALTER TABLE "ProjectTag" DROP CONSTRAINT IF EXISTS "PK_ProjectTag";
                DROP INDEX IF EXISTS "IX_ProjectTag_TagsId";
                ALTER TABLE "ProjectTag" RENAME TO "ProjectTag_old";
                CREATE TABLE "ProjectTag"
                (
                    "ProjectsId" uuid NOT NULL,
                    "TagsId" uuid NOT NULL,
                    CONSTRAINT "PK_ProjectTag" PRIMARY KEY ("ProjectsId", "TagsId"),
                    CONSTRAINT "FK_ProjectTag_Project_ProjectsId" FOREIGN KEY ("ProjectsId") REFERENCES "Project" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ProjectTag_Tag_TagsId" FOREIGN KEY ("TagsId") REFERENCES "Tag" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX "IX_ProjectTag_TagsId" ON "ProjectTag" ("TagsId");
                INSERT INTO "ProjectTag" ("ProjectsId", "TagsId")
                SELECT "ProjectsId", "TagsId" FROM "ProjectTag_old";
                DROP TABLE "ProjectTag_old";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Reordering columns cannot be automatically reverted.");
        }
    }
}
