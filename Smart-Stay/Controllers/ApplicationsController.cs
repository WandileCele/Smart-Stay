using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using Smart_Stay.Services;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class ApplicationsController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IEmailService _emailService;

        public ApplicationsController(SmartDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int? CurrentLandlordId()
        {
            var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idString, out int id) ? id : null;
        }

        // ============================================================
        // LIST APPLICATIONS FOR THIS LANDLORD
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ManageApplications(string status = "Pending")
        {
            var landlordId = CurrentLandlordId();
            if (landlordId == null) return RedirectToAction("Login", "Account");

            var query = _context.RentalApplications
                .Include(a => a.Tenant).ThenInclude(t => t.User)
                .Include(a => a.Property)
                .Include(a => a.Documents)
                .Where(a => a.LandlordId == landlordId);

            if (status != "All")
            {
                query = query.Where(a => a.RentalApplicationStatus == status);
            }

            var applications = await query
                .OrderByDescending(a => a.ApplicationDate)
                .Select(a => new ApplicationReviewViewModel
                {
                    RentalApplicationId = a.RentalApplicationId,
                    TenantName = a.Tenant.User.FirstName + " " + a.Tenant.User.SurName,
                    Email = a.Tenant.User.Email,
                    PhoneNumber = a.Tenant.User.PhoneNo,
                    Employment = a.Tenant.EmploymentStatus,
                    PropertyTitle = a.Property.Title,
                    ApplicationDate = a.ApplicationDate,
                    Status = a.RentalApplicationStatus,
                    LeaseStartDate = a.LeaseStartDate,
                    LeaseEndDate = a.LeaseEndDate,
                    PayslipPath = a.Documents
                        .Where(d => d.DocumentType == "Payslip")
                        .Select(d => d.DocumentPath)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var app in applications)
            {
                app.PayslipFileName = app.PayslipPath == null
                    ? null
                    : Path.GetFileName(app.PayslipPath);
            }

            ViewBag.CurrentStatus = status;
            return View(applications);
        }

        // ============================================================
        // APPROVE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var landlordId = CurrentLandlordId();
            var application = await _context.RentalApplications
                .FirstOrDefaultAsync(a => a.RentalApplicationId == id && a.LandlordId == landlordId);

            if (application == null) return NotFound();

            application.RentalApplicationStatus = "Approved";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application approved.";
            return RedirectToAction(nameof(ManageApplications));
        }

        // ============================================================
        // REJECT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var landlordId = CurrentLandlordId();

            var application = await _context.RentalApplications
                .Include(a => a.Tenant)
                    .ThenInclude(t => t.User)
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a =>
                    a.RentalApplicationId == id &&
                    a.LandlordId == landlordId);

            if (application == null)
                return NotFound();

            if (application.RentalApplicationStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending applications can be rejected.";

                return RedirectToAction(
                    nameof(ManageApplications),
                    new { status = "Pending" });
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] =
                    "Please provide a reason for rejecting the application.";

                return RedirectToAction(
                    nameof(ManageApplications),
                    new { status = "Pending" });
            }

            // Save the rejection to the database
            application.RejectionReason = rejectionReason.Trim();
            application.RentalApplicationStatus = "Rejected";

            await _context.SaveChangesAsync();

            // Get the tenant and property details
            string tenantEmail = application.Tenant.User.Email;
            string tenantName = application.Tenant.User.FirstName;
            string propertyTitle = application.Property.Title;
            string reason = application.RejectionReason;

            // Safely format values for HTML
            string safeTenantName = System.Net.WebUtility.HtmlEncode(tenantName);
            string safePropertyTitle = System.Net.WebUtility.HtmlEncode(propertyTitle);
            string safeReason = System.Net.WebUtility.HtmlEncode(reason)
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>");

            string subject = "Update on Your Smart Stay Rental Application";

            string htmlBody = $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <h2>Smart Stay - Application Update</h2>

            <p>Dear {safeTenantName},</p>

            <p>
                Thank you for applying to rent
                <strong>{safePropertyTitle}</strong>
                through Smart Stay.
            </p>

            <p>
                Unfortunately, your rental application was not successful.
            </p>

            <h3>Reason for rejection</h3>

            <p>{safeReason}</p>

            <p>
                Thank you for your interest in Smart Stay.
                You may continue browsing other available properties on our platform.
            </p>

            <p>Kind regards,<br>Smart Stay Team</p>
        </div>";

            // Send the email to the tenant
            try
            {
                await _emailService.SendEmailAsync(
                    tenantEmail,
                    subject,
                    htmlBody);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "The application was rejected and saved, but the notification email could not be sent.";

                return RedirectToAction(
                    nameof(ManageApplications),
                    new { status = "Pending" });
            }

            TempData["SuccessMessage"] =
                "Application rejected successfully. The tenant has been notified by email.";

            return RedirectToAction(
                nameof(ManageApplications),
                new { status = "Pending" });
        }

        // ============================================================
        // REQUEST MORE INFO -> EMAILS TENANT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestMoreInfo(int id, string message)
        {
            var landlordId = CurrentLandlordId();

            var application = await _context.RentalApplications
                .Include(a => a.Tenant).ThenInclude(t => t.User)
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a => a.RentalApplicationId == id && a.LandlordId == landlordId);

            if (application == null) return NotFound();

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Please enter a message before sending.";
                return RedirectToAction(nameof(ManageApplications));
            }

            var tenantEmail = application.Tenant.User.Email;
            var tenantFirstName = application.Tenant.User.FirstName;

            var htmlBody = $@"
                <p>Hi {tenantFirstName},</p>
                <p>Regarding your rental application for <strong>{application.Property.Title}</strong>,
                the landlord has requested more information:</p>
                <blockquote>{System.Net.WebUtility.HtmlEncode(message)}</blockquote>
                <p>Please reply to this email or log in to Smart Stay to respond.</p>";

            await _emailService.SendEmailAsync(
                tenantEmail,
                $"More information needed - {application.Property.Title}",
                htmlBody);

            TempData["SuccessMessage"] = "Message sent to tenant.";
            return RedirectToAction(nameof(ManageApplications));
        }
    }
}