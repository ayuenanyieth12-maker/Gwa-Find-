using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gwa_Find__cld_.Migrations
{
    /// <inheritdoc />
    public partial class MakeAreaNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_AspNetUsers_SeekerId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Listings_ListingId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_AspNetUsers_SeekerId",
                table: "Inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_Listings_ListingId",
                table: "Inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedById",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Listings_ListingId",
                table: "Reports");

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "AreaSqM",
                table: "Listings",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Amenities",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_AspNetUsers_SeekerId",
                table: "Favorites",
                column: "SeekerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Listings_ListingId",
                table: "Favorites",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_AspNetUsers_SeekerId",
                table: "Inquiries",
                column: "SeekerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_Listings_ListingId",
                table: "Inquiries",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedById",
                table: "Reports",
                column: "ReportedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Listings_ListingId",
                table: "Reports",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_AspNetUsers_SeekerId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Listings_ListingId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_AspNetUsers_SeekerId",
                table: "Inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_Listings_ListingId",
                table: "Inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedById",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Listings_ListingId",
                table: "Reports");

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "AreaSqM",
                table: "Listings",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Amenities",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_AspNetUsers_SeekerId",
                table: "Favorites",
                column: "SeekerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Listings_ListingId",
                table: "Favorites",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_AspNetUsers_SeekerId",
                table: "Inquiries",
                column: "SeekerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_Listings_ListingId",
                table: "Inquiries",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedById",
                table: "Reports",
                column: "ReportedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Listings_ListingId",
                table: "Reports",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
