using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidence_MasterDetails_SinglePage.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applicant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false, defaultValueSql: "('')"),
                    Gender = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValueSql: "('')"),
                    DOB = table.Column<DateOnly>(type: "date", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Qualification = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false, defaultValueSql: "('')"),
                    TotalExperience = table.Column<int>(type: "int", nullable: true),
                    PhotoUrl = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Experience",
                columns: table => new
                {
                    ExperienceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: true),
                    CompanyName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Designation = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    YearsWorked = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experience", x => x.ExperienceId);
                    table.ForeignKey(
                        name: "FK__Experienc__Appli__29572725",
                        column: x => x.ApplicantId,
                        principalTable: "Applicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Experience_ApplicantId",
                table: "Experience",
                column: "ApplicantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Experience");

            migrationBuilder.DropTable(
                name: "Applicant");
        }
    }
}
