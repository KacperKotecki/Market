using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Market.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBannedNoteToAuction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannedNote",
                table: "Auctions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannedNote",
                table: "Auctions");
        }
    }
}
