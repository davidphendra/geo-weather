using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Weather.Modules.Notes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateNotesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityProjections",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityProjections", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "WeatherNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherNotes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityProjections");

            migrationBuilder.DropTable(
                name: "WeatherNotes");
        }
    }
}
