using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using Smart_Stay.Services;
using System.Linq;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IEmailService _emailService;

        public AdminController(SmartDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel();
            int adminUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            model.FirstName = await _context.Users.Where(u => u.UserId == adminUserId).Select(u => u.FirstName).FirstOrDefaultAsync() ?? "Admin";
            model.ProfileImagePath = await _context.Users.Where(u => u.UserId == adminUserId).Select(u => u.ProfileImagePath).FirstOrDefaultAsync();

            model.PendingApplications = await _context.ListingApplications.CountAsync(a => a.ApplicationStatus == "Pending");
            model.ApprovedApplications = await _context.ListingApplications.CountAsync(a => a.ApplicationStatus == "Approved");
            model.RejectedApplications = await _context.ListingApplications.CountAsync(a => a.ApplicationStatus == "Rejected");

            model.Applications = await _context.ListingApplications
                   .Include(a => a.Property)
                   .Include(a => a.Landlord).ThenInclude(l => l.User)
                   .Where(a => a.ApplicationStatus == "Pending")
                   .Select(a => new ListingApplicationCardViewModel
                   {
                       ListingApplicationId = a.ListingApplicationId,
                       PropertyId = a.PropertyId ?? 0,
                       PropertyTitle = a.Property != null ? a.Property.Title : "",
                       LandlordName = a.Landlord != null ? a.Landlord.User.FirstName + " " + a.Landlord.User.SurName : "",
                       Location = a.Property != null ? a.Property.Location : "",
                       Price = a.Property != null ? a.Property.Price : 0,
                       PropertyType = a.Property != null ? a.Property.PropertyType : "",
                       ApplicationDate = a.ApplicationDate,
                       ApplicationStatus = a.ApplicationStatus
                   }).ToListAsync();

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.ListingApplications.Include(a => a.Property).FirstOrDefaultAsync(a => a.ListingApplicationId == id);
            if (application == null) return NotFound();

            application.ApplicationStatus = "Approved";
            application.RejectionReason = null;
            if (application.Property != null) application.Property.Status = "Available";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var application = await _context.ListingApplications
               .Include(a => a.Property).Include(a => a.Landlord).ThenInclude(l => l.User)
               .FirstOrDefaultAsync(a => a.ListingApplicationId == id);

            if (application == null) return NotFound();

            if (application.ApplicationStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending listing applications can be rejected.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Please provide a reason for rejection.";
                return RedirectToAction(nameof(ApplicationDetails), new { id });
            }

            reason = reason.Trim();
            application.ApplicationStatus = "Rejected";
            application.RejectionReason = reason;
            if (application.Property != null) application.Property.Status = "Rejected";
            await _context.SaveChangesAsync();

            var landlordEmail = application.Landlord?.User?.Email;
            if (string.IsNullOrWhiteSpace(landlordEmail))
            {
                TempData["ErrorMessage"] = "The listing was rejected and saved, but the landlord's email address could not be found.";
                return RedirectToAction(nameof(Dashboard));
            }

            var propertyTitle = application.Property?.Title ?? "your property";
            var landlordFirstName = application.Landlord?.User?.FirstName ?? "there";
            var safeName = System.Net.WebUtility.HtmlEncode(landlordFirstName);
            var safeTitle = System.Net.WebUtility.HtmlEncode(propertyTitle);
            var safeReason = System.Net.WebUtility.HtmlEncode(reason).Replace("\r\n", "<br>").Replace("\n", "<br>");
            var subject = $"Listing Rejected - {propertyTitle}";
            var htmlBody = $@"<div style='font-family:Arial,sans-serif;line-height:1.6;color:#333;'><h2>Smart Stay - Listing Application Update</h2><p>Dear {safeName},</p><p>Unfortunately, your property listing <strong>{safeTitle}</strong> has not been approved.</p><h3>Reason for rejection</h3><p>{safeReason}</p><p>Please review the reason above and make any necessary corrections before resubmitting.</p><p>Kind regards,<br/>Smart Stay Team</p></div>";

            try { await _emailService.SendEmailAsync(landlordEmail, subject, htmlBody); }
            catch (Exception) { TempData["ErrorMessage"] = "The listing was rejected and saved, but the notification email could not be sent."; return RedirectToAction(nameof(Dashboard)); }

            TempData["SuccessMessage"] = "Listing rejected successfully. The landlord has been notified by email.";
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application = await _context.ListingApplications
               .Include(a => a.Property).Include(a => a.Landlord).ThenInclude(l => l.User)
               .FirstOrDefaultAsync(a => a.ListingApplicationId == id);

            if (application == null || application.Property == null) return NotFound();

            var documents = await _context.Documents.Where(d => d.ListingApplication == id).ToListAsync();

            var model = new ListingApplicationReviewViewModel
            {
                ListingApplicationId = application.ListingApplicationId,
                ApplicationStatus = application.ApplicationStatus,
                ApplicationDate = application.ApplicationDate,
                RejectionReason = application.RejectionReason,
                PropertyId = application.Property.PropertyId,
                Title = application.Property.Title,
                Description = application.Property.Description,
                Location = application.Property.Location,
                Address = application.Property.Address,
                Latitude = application.Property.Latitude,
                Longitude = application.Property.Longitude,
                Price = application.Property.Price,
                PropertyType = application.Property.PropertyType,
                Bedrooms = application.Property.Bedrooms,
                Bathrooms = application.Property.Bathrooms,
                LandlordName = application.Landlord != null ? application.Landlord.User.FirstName + " " + application.Landlord.User.SurName : "",
                AffidavitPath = documents.Where(d => d.DocumentType == "Affidavit").Select(d => d.DocumentPath).FirstOrDefault(),
                ImagePaths = documents.Where(d => d.DocumentType == "Image").Select(d => d.DocumentPath).ToList()
            };
            return View(model);
        }

        public async Task<IActionResult> ManageLandlords()
        {
            var landlords = await _context.Landlords.Include(l => l.User).Include(l => l.Properties).ToListAsync();
            return View(landlords);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            user.IsSuspended = !user.IsSuspended;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsSuspended ? "User suspended successfully." : "User unsuspended successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Account");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return RedirectToAction("Login", "Account");
            var model = new UpdateProfileViewModel { FirstName = user.FirstName, SurName = user.SurName, Email = user.Email, PhoneNo = user.PhoneNo, CurrentImagePath = user.ProfileImagePath };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Account");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return RedirectToAction("Login", "Account");

            bool passwordEntered = !string.IsNullOrWhiteSpace(model.NewPassword);
            bool confirmPasswordEntered = !string.IsNullOrWhiteSpace(model.ConfirmPassword);
            if (passwordEntered || confirmPasswordEntered)
            {
                if (!passwordEntered) ModelState.AddModelError("NewPassword", "Please enter a new password.");
                if (!confirmPasswordEntered) ModelState.AddModelError("ConfirmPassword", "Please confirm your password.");
                if (passwordEntered && confirmPasswordEntered && model.NewPassword != model.ConfirmPassword) ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                if (passwordEntered && model.NewPassword!.Length < 6) ModelState.AddModelError("NewPassword", "Password must be at least 6 characters.");
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != userId);
            if (emailExists) ModelState.AddModelError("Email", "This email is already being used.");

            if (!ModelState.IsValid) { model.CurrentImagePath = user.ProfileImagePath; return View(model); }

            user.FirstName = model.FirstName.Trim();
            user.SurName = model.SurName.Trim();
            user.Email = model.Email.Trim();
            user.PhoneNo = model.PhoneNo.Trim();
            if (!string.IsNullOrWhiteSpace(model.NewPassword)) user.Password = model.NewPassword;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ProfileImage", "Only JPG, JPEG, PNG, GIF and WEBP files are allowed.");
                    model.CurrentImagePath = user.ProfileImagePath; return View(model);
                }
                if (model.ProfileImage.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfileImage", "Image must be smaller than 5MB.");
                    model.CurrentImagePath = user.ProfileImagePath; return View(model);
                }
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-images");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) { await model.ProfileImage.CopyToAsync(stream); }
                user.ProfileImagePath = "/uploads/profile-images/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(UpdateProfile));
        }
    }
}