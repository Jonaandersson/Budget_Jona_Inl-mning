using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget_Jona_Inlämning.Migrations
{
    /// <inheritdoc />
    public partial class BoolTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncome",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncome",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Categories");
        }
    }
}
