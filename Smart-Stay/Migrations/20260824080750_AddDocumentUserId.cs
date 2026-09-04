using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Stay.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    firstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    surName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    phoneNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    date_Registered = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin", x => x.userID);
                    table.ForeignKey(
                        name: "FK_Admin_User",
                        column: x => x.userID,
                        principalTable: "User",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateTable(
                name: "Landlord",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false),
                    verification_status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Landlord", x => x.userID);
                    table.ForeignKey(
                        name: "FK_Landlord_User",
                        column: x => x.userID,
                        principalTable: "User",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false),
                    employment_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "Employed")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.userID);
                    table.ForeignKey(
                        name: "FK_Tenant_User",
                        column: x => x.userID,
                        principalTable: "User",
                        principalColumn: "userID");
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
                    date_listed = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bedrooms = table.Column<int>(type: "int", nullable: true),
                    bathrooms = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Property", x => x.propertyID);
                    table.ForeignKey(
                        name: "FK_Property_Landlord",
                        column: x => x.landlordID,
                        principalTable: "Landlord",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateTable(
                name: "ListingApplication",
                columns: table => new
                {
                    listingApplicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    adminID = table.Column<int>(type: "int", nullable: false),
                    LandlordID = table.Column<int>(type: "int", nullable: true),
                    propertyID = table.Column<int>(type: "int", nullable: true),
                    application_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    application_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingApplication", x => x.listingApplicationID);
                    table.ForeignKey(
                        name: "FK_ListingApplication_Admin",
                        column: x => x.adminID,
                        principalTable: "Admin",
                        principalColumn: "userID");
                    table.ForeignKey(
                        name: "FK_ListingApplication_Landlord",
                        column: x => x.LandlordID,
                        principalTable: "Landlord",
                        principalColumn: "userID");
                    table.ForeignKey(
                        name: "FK_ListingApplication_Property",
                        column: x => x.propertyID,
                        principalTable: "Properties",
                        principalColumn: "propertyID");
                });

            migrationBuilder.CreateTable(
                name: "rentalApplication",
                columns: table => new
                {
                    rentalApplicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenantID = table.Column<int>(type: "int", nullable: false),
                    application_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    rentalApplicationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Id_Number = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    landlordID = table.Column<int>(type: "int", nullable: true),
                    propertyID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentalApplication", x => x.rentalApplicationID);
                    table.ForeignKey(
                        name: "FK_rentalApplication_Landlord",
                        column: x => x.landlordID,
                        principalTable: "Landlord",
                        principalColumn: "userID");
                    table.ForeignKey(
                        name: "FK_rentalApplication_Property",
                        column: x => x.propertyID,
                        principalTable: "Properties",
                        principalColumn: "propertyID");
                    table.ForeignKey(
                        name: "FK_rentalApplication_Tenant1",
                        column: x => x.tenantID,
                        principalTable: "Tenant",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateTable(
                name: "review",
                columns: table => new
                {
                    reviewID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    propertyID = table.Column<int>(type: "int", nullable: false),
                    tenantID = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    rating = table.Column<byte>(type: "tinyint", nullable: false),
                    reviewDate = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(CONVERT([date],getdate()))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review", x => x.reviewID);
                    table.ForeignKey(
                        name: "FK_review_Property",
                        column: x => x.propertyID,
                        principalTable: "Properties",
                        principalColumn: "propertyID");
                    table.ForeignKey(
                        name: "FK_review_Tenant",
                        column: x => x.tenantID,
                        principalTable: "Tenant",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    documentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userID = table.Column<int>(type: "int", nullable: true),
                    listingApplication = table.Column<int>(type: "int", nullable: true),
                    rentalApplicationID = table.Column<int>(type: "int", nullable: true),
                    document_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    upload_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    documentPath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Document", x => x.documentID);
                    table.ForeignKey(
                        name: "FK_Document_ListingApplication",
                        column: x => x.listingApplication,
                        principalTable: "ListingApplication",
                        principalColumn: "listingApplicationID");
                    table.ForeignKey(
                        name: "FK_Document_User",
                        column: x => x.userID,
                        principalTable: "User",
                        principalColumn: "userID");
                    table.ForeignKey(
                        name: "FK_Document_rentalApplication",
                        column: x => x.rentalApplicationID,
                        principalTable: "rentalApplication",
                        principalColumn: "rentalApplicationID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Document_listingApplication",
                table: "Document",
                column: "listingApplication");

            migrationBuilder.CreateIndex(
                name: "IX_Document_rentalApplicationID",
                table: "Document",
                column: "rentalApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_Document_userID",
                table: "Document",
                column: "userID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingApplication_adminID",
                table: "ListingApplication",
                column: "adminID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingApplication_LandlordID",
                table: "ListingApplication",
                column: "LandlordID");

            migrationBuilder.CreateIndex(
                name: "IX_ListingApplication_propertyID",
                table: "ListingApplication",
                column: "propertyID");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_landlordID",
                table: "Properties",
                column: "landlordID");

            migrationBuilder.CreateIndex(
                name: "IX_rentalApplication_landlordID",
                table: "rentalApplication",
                column: "landlordID");

            migrationBuilder.CreateIndex(
                name: "IX_rentalApplication_propertyID",
                table: "rentalApplication",
                column: "propertyID");

            migrationBuilder.CreateIndex(
                name: "IX_rentalApplication_tenantID",
                table: "rentalApplication",
                column: "tenantID");

            migrationBuilder.CreateIndex(
                name: "IX_review_propertyID",
                table: "review",
                column: "propertyID");

            migrationBuilder.CreateIndex(
                name: "IX_review_tenantID",
                table: "review",
                column: "tenantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Document");

            migrationBuilder.DropTable(
                name: "review");

            migrationBuilder.DropTable(
                name: "ListingApplication");

            migrationBuilder.DropTable(
                name: "rentalApplication");

            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Tenant");

            migrationBuilder.DropTable(
                name: "Landlord");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
