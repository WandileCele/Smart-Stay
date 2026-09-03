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
        // CHECK IF TENANT IS BLOCKED FROM REAPPLYING
        // Returns the blocking application (Pending or Approved),
        // or null if the tenant is free to apply.
        // ============================================================

        private async Task<RentalApplication?> GetBlockingApplication(int tenantId, int propertyId)
        {
            var existing = await _context.RentalApplications
                .Where(a => a.TenantId == tenantId && a.PropertyId == propertyId)
                .OrderByDescending(a => a.ApplicationDate)
                .FirstOrDefaultAsync();

            if (existing == null)
                return null;

            if (existing.RentalApplicationStatus == "Rejected")
                return null;

            return existing;
        }


        // ============================================================
        // SHOW RENTAL APPLICATION FORM
        // ============================================================

        [HttpGet]
        public async Task<IActionResult>
    Apply(int propertyId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId);

            if (property == null)
            {
                return NotFound();
            }

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.UserId == tenantId);

            if (tenant == null)
            {
                return NotFound();
            }

            var blocking = await GetBlockingApplication(tenantId, propertyId);
            if (blocking != null)
            {
                TempData["ErrorMessage"] = blocking.RentalApplicationStatus == "Pending"
                    ? "You already have a pending application for this property."
                    : "You already have an approved application for this property.";
                return RedirectToAction("Dashboard", "Tenant");
            }

            // Form starts empty except for the property name.
            // The applicant types everything else themselves.
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
        public async Task<IActionResult> Apply(RentalApplicationFormViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }

            model.PropertyTitle = property.Title;

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.UserId == tenantId);

            if (tenant == null)
            {
                return NotFound();
            }

            var blocking = await GetBlockingApplication(tenantId, model.PropertyId);
            if (blocking != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    blocking.RentalApplicationStatus == "Pending"
                        ? "You already have a pending application for this property."
                        : "You already have an approved application for this property.");
                return View(model);
            }


            // ========================================================
            // MANUAL VALIDATION
            // (Kept out of data annotations on purpose so there is
            // exactly one place these rules are enforced.)
            // ========================================================

            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(
                    nameof(model.AcceptTerms),
                    "You must accept the Terms and Conditions before submitting.");
            }

            if (model.LeaseStartDate == null)
            {
                ModelState.AddModelError(
                    nameof(model.LeaseStartDate),
                    "Lease start date is required.");
            }
            else if (model.LeaseStartDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError(
                    nameof(model.LeaseStartDate),
                    "Lease start date cannot be in the past.");
            }

            if (model.LeaseEndDate == null)
            {
                ModelState.AddModelError(
                    nameof(model.LeaseEndDate),
                    "Lease end date is required.");
            }
            else if (model.LeaseStartDate != null && model.LeaseEndDate <= model.LeaseStartDate)
            {
                ModelState.AddModelError(
                    nameof(model.LeaseEndDate),
                    "Lease end date must be after the start date.");
            }

            if (model.Payslip == null || model.Payslip.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Payslip),
                    "Please upload your payslip.");
                nameof(model.AcceptTerms),
                "You must accept the Terms and Conditions before submitting.");
            }

            if (model.Payslip == null || model.Payslip.Length == 0)
            {
                ModelState.AddModelError(
                nameof(model.Payslip),
                "Please upload your payslip.");
            }
            else
            {
                if (model.Payslip.Length > 3 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(model.Payslip),
                        "Payslip must be 3MB or smaller.");
                }

                var extension = Path.GetExtension(model.Payslip.FileName)
                    .ToLowerInvariant();

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(
                        nameof(model.Payslip),
                        "Only PDF payslips are allowed.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Sends the user back to the same form with error
                // messages next to each field via asp-validation-for.
                return View(model);
            }


            // ========================================================
            // CREATE RENTAL APPLICATION
            // ========================================================

            var rentalApplication = new RentalApplication
            {
                TenantId = tenantId,
                ApplicationDate = DateOnly.FromDateTime(DateTime.Now),
                RentalApplicationStatus = "Pending",
                IdNumber = model.IdNumber,
                LandlordId = property.LandlordId,
                PropertyId = model.PropertyId,
                LeaseStartDate = model.LeaseStartDate,
                LeaseEndDate = model.LeaseEndDate
            };

            _context.RentalApplications.Add(rentalApplication);
            await _context.SaveChangesAsync();


            // ========================================================
            // SAVE PAYSLIP
            // ========================================================

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath, "uploads", "payslips");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() + ".pdf";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.Payslip!.CopyToAsync(stream);
            }

            var document = new Smart_Stay.Models.Document
            {
                RentalApplicationId = rentalApplication.RentalApplicationId,
                DocumentType = "Payslip",
                UploadDate = DateOnly.FromDateTime(DateTime.Now),
                DocumentPath = "/uploads/payslips/" + fileName
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();


            // ========================================================
            // GENERATE + DOWNLOAD PDF
            // ========================================================
            // We do NOT return the PDF directly from this POST action,
            // and we do NOT put it in TempData/cookies either (a PDF is
            // far too big for a cookie — that's what caused the
            // HTTP 431 "request header too large" error). Instead we
            // write the PDF to a small temp folder on disk, keyed by
            // the application's ID, and redirect to a normal
            // confirmation page. The redirect completes a real page
            // navigation, which clears any loading overlay, and gives
            // the applicant an actual "Submitted!" message.

            QuestPDF.Settings.License = LicenseType.Community;

            byte[] pdf = GenerateApplicationPdf(rentalApplication, model);

            var pdfFolder = Path.Combine(
            _environment.ContentRootPath, "App_Data", "GeneratedPdfs");

            if (!Directory.Exists(pdfFolder))
            {
                Directory.CreateDirectory(pdfFolder);
            }

            var pdfPath = Path.Combine(
            pdfFolder, $"application_{rentalApplication.RentalApplicationId}.pdf");

            await System.IO.File.WriteAllBytesAsync(pdfPath, pdf);

            return RedirectToAction(
            nameof(Success), new { id = rentalApplication.RentalApplicationId });
        }


        // ============================================================
        // CONFIRMATION PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Success(int id)
        {
            var pdfPath = Path.Combine(
            _environment.ContentRootPath, "App_Data", "GeneratedPdfs",
            $"application_{id}.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                // Nothing to show (e.g. link opened directly, or the
                // file was already cleaned up) — send them back to the form.
                return RedirectToAction(nameof(Apply));
            }

            ViewBag.ApplicationId = id;

            return View();
        }


        // ============================================================
        // DOWNLOAD THE GENERATED PDF (separate, on-demand action)
        // ============================================================

        [HttpGet]
        public IActionResult DownloadPdf(int id)
        {
            var pdfPath = Path.Combine(
            _environment.ContentRootPath, "App_Data", "GeneratedPdfs",
            $"application_{id}.pdf");

            if (!System.IO.File.Exists(pdfPath))
            {
                return NotFound();
            }

            var pdfBytes = System.IO.File.ReadAllBytes(pdfPath);

            return File(pdfBytes, "application/pdf", "SmartStay_Rental_Application.pdf");
        }


        // ============================================================
        // CREATE APPLICATION PDF
        // ============================================================

        private byte[] GenerateApplicationPdf(
        RentalApplication application,
        RentalApplicationFormViewModel model)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Column(column =>
                    {
                        column.Item().Text("SMART STAY").Bold().FontSize(24);
                        column.Item().Text("RENTAL APPLICATION").Bold().FontSize(18);
                        column.Item().LineHorizontal(1);
                    });

                    page.Content().PaddingTop(20).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text($"Property: {model.PropertyTitle}").Bold();
                        column.Item().Text($"Application ID: {application.RentalApplicationId}");
                        column.Item().Text($"Application Date: {application.ApplicationDate}");

                        column.Item().PaddingTop(15).Text("APPLICANT DETAILS").Bold().FontSize(15);
                        column.Item().Text($"First Name: {model.FirstName}");
                        column.Item().Text($"Last Name: {model.LastName}");
                        column.Item().Text($"ID Number: {model.IdNumber}");
                        column.Item().Text($"Phone Number: {model.PhoneNumber}");
                        column.Item().Text($"Email: {model.Email}");
                        column.Item().Text($"Employment: {model.Employment}");

                        column.Item().PaddingTop(15).Text("APPLICATION STATUS").Bold().FontSize(15);
                        column.Item().Text($"Status: {application.RentalApplicationStatus}");
                        column.Item().Text($"Lease: {model.LeaseStartDate} to {model.LeaseEndDate}");
                        column.Item().Text("Payslip: Uploaded successfully");
                        column.Item().Text("Terms and Conditions: Accepted");

                        column.Item().PaddingTop(30)
                            .Text("Thank you for submitting your rental application to Smart Stay.");
                    });

                    page.Footer().AlignCenter().Text("Smart Stay - Rental Application");
                });
            });

            return document.GeneratePdf();
        }
    }
}
