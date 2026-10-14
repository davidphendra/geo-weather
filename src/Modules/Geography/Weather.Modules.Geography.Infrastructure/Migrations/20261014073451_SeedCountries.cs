using Microsoft.EntityFrameworkCore.Migrations;
using Weather.Modules.Geography.Infrastructure.Migrations.Seed;

#nullable disable

namespace Weather.Modules.Geography.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCountries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SeedScripts.Countries);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Countries\";");
        }
    }
}
