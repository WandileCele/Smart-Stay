
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class RentalApplicationsController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RentalApplicationsController(
            SmartDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // ============================================================
        // SHOW RENTAL APPLICATION FORM
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Apply(int propertyId)
        {
            // Get logged-in tenant
            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find property
            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == propertyId);

            if (property == null)
            {
                return NotFound();
            }

            // Make sure tenant exists
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.UserId == tenantId);

            if (tenant == null)
            {
                return NotFound();
            }

            // IMPORTANT:
            // Do NOT fill in the tenant's personal information.
            // The user must type it into the form.

            var model = new RentalApplicationFormViewModel
            {
                PropertyId = property.PropertyId,
                PropertyTitle = property.Title
            };

            return View(model);
        }


        // ============================================================
        // SUBMIT RENTAL APPLICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            RentalApplicationFormViewModel model)
        {
            // ========================================================
            // GET LOGGED-IN TENANT
            // ========================================================

            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }


            // ========================================================
            // FIND PROPERTY
            // ========================================================

            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }


            // ========================================================
            // FIND TENANT
            // ========================================================

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.UserId == tenantId);

            if (tenant == null)
            {
                return NotFound();
            }


            // Put the property title back into the model.
            // We DO NOT overwrite the information typed by the user.
            model.PropertyTitle = property.Title;


            // ========================================================
            // PAYSLIP VALIDATION
            // ========================================================

            if (model.Payslip == null ||
                model.Payslip.Length == 0)
            {
                ModelState.AddModelError(
                    "Payslip",
                    "Please upload your payslip.");
            }
            else
            {
                // Maximum 3MB
                if (model.Payslip.Length > 3 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "Payslip",
                        "Payslip must be 3MB or smaller.");
                }

                // PDF only
                var extension =
                    Path.GetExtension(
                        model.Payslip.FileName)
                    .ToLowerInvariant();

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(
                        "Payslip",
                        "Only PDF payslips are allowed.");
                }
            }


            // ========================================================
            // TERMS & CONDITIONS
            // ========================================================

            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(
                    "AcceptTerms",
                    "You must accept the Terms and Conditions before submitting.");
            }


            // ========================================================
            // VALIDATE FORM
            // ========================================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ========================================================
            // CREATE RENTAL APPLICATION
            // ========================================================

            var rentalApplication = new RentalApplication
            {
                TenantId = tenantId,

                ApplicationDate =
                    DateOnly.FromDateTime(DateTime.Now),

                RentalApplicationStatus = "Pending",

                IdNumber = model.IdNumber,

                LandlordId = property.LandlordId,

                PropertyId = model.PropertyId
            };

            _context.RentalApplications.Add(
                rentalApplication);

            await _context.SaveChangesAsync();


            // ========================================================
            // SAVE PAYSLIP
            // ========================================================

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "payslips");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }


            // Generate unique filename
            var fileName =
                Guid.NewGuid().ToString()
                + ".pdf";


            var filePath = Path.Combine(
                uploadsFolder,
                fileName);


            // Save physical PDF
            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await model.Payslip.CopyToAsync(stream);
            }


            // ========================================================
            // SAVE DOCUMENT INFORMATION
            // ========================================================

            var document =
                new Smart_Stay.Models.Document
                {
                    RentalApplicationId =
                        rentalApplication.RentalApplicationId,

                    DocumentType = "Payslip",

                    UploadDate =
                        DateOnly.FromDateTime(
                            DateTime.Now),

                    DocumentPath =
                        "/uploads/payslips/"
                        + fileName
                };


            _context.Documents.Add(document);

            await _context.SaveChangesAsync();


            // ========================================================
            // GENERATE APPLICATION PDF
            // ========================================================

            QuestPDF.Settings.License =
                QuestPDF.Infrastructure.LicenseType.Community;


            byte[] pdf = GenerateApplicationPdf(
                rentalApplication,
                model);


            // ========================================================
            // DOWNLOAD PDF
            // ========================================================

            return File(
                pdf,
                "application/pdf",
                "SmartStay_Rental_Application.pdf");
        }


        // ============================================================
        // CREATE APPLICATION PDF
        // ============================================================

        private byte[] GenerateApplicationPdf(
            RentalApplication application,
            RentalApplicationFormViewModel model)
        {
            var document =
                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);

                        page.Margin(40);


                        // ====================================================
                        // HEADER
                        // ====================================================

                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .Text("SMART STAY")
                                    .Bold()
                                    .FontSize(24);

                                column.Item()
                                    .Text("RENTAL APPLICATION")
                                    .Bold()
                                    .FontSize(18);

                                column.Item()
                                    .LineHorizontal(1);
                            });


                        // ====================================================
                        // CONTENT
                        // ====================================================

                        page.Content()
                            .PaddingTop(20)
                            .Column(column =>
                            {
                                column.Spacing(10);


                                column.Item()
                                    .Text(
                                        $"Property: {model.PropertyTitle}")
                                    .Bold();


                                column.Item()
                                    .Text(
                                        $"Application ID: {application.RentalApplicationId}");


                                column.Item()
                                    .Text(
                                        $"Application Date: {application.ApplicationDate}");


                                // APPLICANT DETAILS

                                column.Item()
                                    .PaddingTop(15)
                                    .Text("APPLICANT DETAILS")
                                    .Bold()
                                    .FontSize(15);


                                column.Item()
                                    .Text(
                                        $"First Name: {model.FirstName}");


                                column.Item()
                                    .Text(
                                        $"Last Name: {model.LastName}");


                                column.Item()
                                    .Text(
                                        $"ID Number: {model.IdNumber}");


                                column.Item()
                                    .Text(
                                        $"Phone Number: {model.PhoneNumber}");


                                column.Item()
                                    .Text(
                                        $"Email: {model.Email}");


                                column.Item()
                                    .Text(
                                        $"Employment: {model.Employment}");


                                // APPLICATION STATUS

                                column.Item()
                                    .PaddingTop(15)
                                    .Text("APPLICATION STATUS")
                                    .Bold()
                                    .FontSize(15);


                                column.Item()
                                    .Text(
                                        $"Status: {application.RentalApplicationStatus}");


                                column.Item()
                                    .Text(
                                        "Payslip: Uploaded successfully");


                                column.Item()
                                    .Text(
                                        "Terms and Conditions: Accepted");


                                column.Item()
                                    .PaddingTop(30)
                                    .Text(
                                        "Thank you for submitting your rental application to Smart Stay.");
                            });


                        // ====================================================
                        // FOOTER
                        // ====================================================

                        page.Footer()
                            .AlignCenter()
                            .Text(
                                "Smart Stay - Rental Application");
                    });
                });


            return document.GeneratePdf();
        }
    }
}

