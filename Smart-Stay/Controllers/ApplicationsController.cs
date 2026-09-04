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
        public async Task<IActionResult> Reject(int id)
        {
            var landlordId = CurrentLandlordId();
            var application = await _context.RentalApplications
                .FirstOrDefaultAsync(a => a.RentalApplicationId == id && a.LandlordId == landlordId);

            if (application == null) return NotFound();

            application.RentalApplicationStatus = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application rejected.";
            return RedirectToAction(nameof(ManageApplications));
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