using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Stay.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "Landlords",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    verification_Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Landlords", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    propertyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    landlordID = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "money", nullable: false),
                    propertyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date_listed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    bedrooms = table.Column<int>(type: "int", nullable: true),
                    bathrooms = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.propertyID);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employment_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "ListingApplications",
                columns: table => new
                {
                    listingApplicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    adminID = table.Column<int>(type: "int", nullable: false),
                    landlordID = table.Column<int>(type: "int", nullable: false),
                    propertyID = table.Column<int>(type: "int", nullable: false),
                    application_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    application_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingApplications", x => x.listingApplicationID);
                    table.ForeignKey(
                        name: "FK_ListingApplications_Admins_adminID",
                        column: x => x.adminID,
                        principalTable: "Admins",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingApplications_Landlords_landlordID",
                        column: x => x.landlordID,
                        principalTable: "Landlords",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingApplications_Properties_propertyID",
                        column: x => x.propertyID,
                        principalTable: "Properties",
                        principalColumn: "propertyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentalApplications",
                columns: table => new
                {
                    rentalApplication = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenantID = table.Column<int>(type: "int", nullable: false),
                    application_date = table.Column<DateOnly>(type: "date", nullable: false),
                    rentalApplicationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    id_Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    landlordID = table.Column<int>(type: "int", nullable: false),
                    propertyID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalApplications", x => x.rentalApplication);
                    table.ForeignKey(
                        name: "FK_RentalApplications_Landlords_landlordID",
                        column: x => x.landlordID,
                        principalTable: "Landlords",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalApplications_Properties_propertyID",
                        column: x => x.propertyID,
                        principalTable: "Properties",
                        principalColumn: "propertyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalApplications_Tenants_tenantID",
                        column: x => x.tenantID,
                        principalTable: "Tenants",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    reviewID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    propertyID = table.Column<int>(type: "int", nullable: false),
                    tenantID = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false),
                    reviewDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.reviewID);
                    table.ForeignKey(
                        name: "FK_Reviews_Properties_propertyID",
                        column: x => x.propertyID,
                        principalTable: "Properties",
                        principalColumn: "propertyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Tenants_tenantID",
                        column: x => x.tenantID,
                        principalTable: "Tenants",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    documentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    listingApplicationID = table.Column<int>(type: "int", nullable: false),
                    rentalApplicationID = table.Column<int>(type: "int", nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    upload_date = table.Column<DateOnly>(type: "date", nullable: false),
                    documentPath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.documentID);
                    table.ForeignKey(
                        name: "FK_Documents_ListingApplications_listingApplicationID",
                        column: x => x.listingApplicationID,
                        principalTable: "ListingApplications",
                        principalColumn: "listingApplicationID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_RentalApplications_rentalApplicationID",
                        column: x => x.rentalApplicationID,
                        principalTable: "RentalApplications",
                        principalColumn: "rentalApplication",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_listingApplicationID",
                table: "Documents",
                column: "listingApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_rentalApplicationID",
                table: "Documents",
                column: "rentalApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingApplications_adminID",
                table: "ListingApplications",
                column: "adminID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingApplications_landlordID",
                table: "ListingApplications",
                column: "landlordID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingApplications_propertyID",
                table: "ListingApplications",
                column: "propertyID");

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_landlordID",
                table: "RentalApplications",
                column: "landlordID");

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_propertyID",
                table: "RentalApplications",
                column: "propertyID");

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_tenantID",
                table: "RentalApplications",
                column: "tenantID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_propertyID",
                table: "Reviews",
                column: "propertyID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_tenantID",
                table: "Reviews",
                column: "tenantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "ListingApplications");

            migrationBuilder.DropTable(
                name: "RentalApplications");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Landlords");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
