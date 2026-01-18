using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Market.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCompanySaleToAuction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompanySale",
                table: "Auctions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompanySale",
                table: "Auctions");
        }
    }
}
