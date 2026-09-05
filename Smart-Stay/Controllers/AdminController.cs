using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Linq;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SmartDbContext _context;
        public AdminController(SmartDbContext context) { _context = context; }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel();
            int adminUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FirstName = await _context.Users.Where(u => u.UserId == adminUserId).Select(u => u.FirstName).FirstOrDefaultAsync() ?? "Admin";
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
            if (application.Property != null) application.Property.Status = "Available"; // FIXED: was "Approved" which broke your filters
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var application = await _context.ListingApplications.Include(a => a.Property).FirstOrDefaultAsync(a => a.ListingApplicationId == id);
            if (application == null) return NotFound();
            application.ApplicationStatus = "Rejected";
            if (application.Property != null) application.Property.Status = "Rejected";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application = await _context.ListingApplications.Include(a => a.Property).Include(a => a.Landlord).ThenInclude(l => l.User).FirstOrDefaultAsync(a => a.ListingApplicationId == id);
            if (application == null || application.Property == null) return NotFound();
            var documents = await _context.Documents.Where(d => d.ListingApplication == id).ToListAsync();
            var model = new ListingApplicationReviewViewModel
            {
                ListingApplicationId = application.ListingApplicationId,
                ApplicationStatus = application.ApplicationStatus,
                ApplicationDate = application.ApplicationDate,
                PropertyId = application.Property.PropertyId,
                Title = application.Property.Title,
                Description = application.Property.Description,
                Location = application.Property.Location,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLandlord(int id)
        {
            var landlord = await _context.Landlords.Include(l => l.User).FirstOrDefaultAsync(l => l.UserId == id);
            if (landlord == null) return NotFound();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var properties = await _context.Properties.Where(p => p.LandlordId == id).ToListAsync();
                var propertyIds = properties.Select(p => p.PropertyId).ToList();
                if (propertyIds.Any())
                {
                    var reviews = await _context.Reviews.Where(r => propertyIds.Contains(r.PropertyId)).ToListAsync();
                    _context.Reviews.RemoveRange(reviews);
                }
                var listingApplications = await _context.ListingApplications.Where(a => a.LandlordId == id).ToListAsync();
                var listingApplicationIds = listingApplications.Select(a => a.ListingApplicationId).ToList();
                if (listingApplicationIds.Any())
                {
                    var listingDocuments = await _context.Documents.Where(d => d.ListingApplication != null && listingApplicationIds.Contains(d.ListingApplication.Value)).ToListAsync();
                    _context.Documents.RemoveRange(listingDocuments);
                }
                _context.ListingApplications.RemoveRange(listingApplications);
                var rentalApplications = await _context.RentalApplications.Where(r => r.LandlordId == id).ToListAsync();
                var rentalApplicationIds = rentalApplications.Select(r => r.RentalApplicationId).ToList();
                if (rentalApplicationIds.Any())
                {
                    var rentalDocuments = await _context.Documents.Where(d => d.RentalApplicationId.HasValue && rentalApplicationIds.Contains(d.RentalApplicationId.Value)).ToListAsync();
                    _context.Documents.RemoveRange(rentalDocuments);
                }
                _context.RentalApplications.RemoveRange(rentalApplications);
                _context.Properties.RemoveRange(properties);
                _context.Landlords.Remove(landlord);
                _context.Users.Remove(landlord.User);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return RedirectToAction(nameof(ManageLandlords));
            }
            catch { await transaction.RollbackAsync(); throw; }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Delete documents via ExecuteDelete - no.ToListAsync() so no Data is Null crash
                var rentalIdsForUser = await _context.RentalApplications
                  .Where(r => r.TenantId == id)
                  .Select(r => r.RentalApplicationId)
                  .ToListAsync();

                if (rentalIdsForUser.Any())
                {
                    await _context.Documents
                      .Where(d => d.RentalApplicationId != null && rentalIdsForUser.Contains(d.RentalApplicationId.Value))
                      .ExecuteDeleteAsync();
                }

                await _context.Reviews.Where(r => r.TenantId == id).ExecuteDeleteAsync();
                await _context.RentalApplications.Where(r => r.TenantId == id).ExecuteDeleteAsync();
                await _context.Tenants.Where(t => t.UserId == id).ExecuteDeleteAsync();

                // Landlord side
                var propIds = await _context.Properties.Where(p => p.LandlordId == id).Select(p => p.PropertyId).ToListAsync();
                if (propIds.Any())
                {
                    await _context.Reviews.Where(r => propIds.Contains(r.PropertyId)).ExecuteDeleteAsync();

                    var rentalIdsByProps = await _context.RentalApplications.Where(r => propIds.Contains(r.PropertyId)).Select(r => r.RentalApplicationId).ToListAsync();
                    if (rentalIdsByProps.Any())
                    {
                        await _context.Documents.Where(d => d.RentalApplicationId != null && rentalIdsByProps.Contains(d.RentalApplicationId.Value)).ExecuteDeleteAsync();
                    }
                    await _context.RentalApplications.Where(r => propIds.Contains(r.PropertyId)).ExecuteDeleteAsync();
                }

                var listingIds = await _context.ListingApplications.Where(a => a.LandlordId == id).Select(a => a.ListingApplicationId).ToListAsync();
                if (listingIds.Any())
                {
                    await _context.Documents.Where(d => d.ListingApplication != null && listingIds.Contains(d.ListingApplication.Value)).ExecuteDeleteAsync();
                }
                await _context.ListingApplications.Where(a => a.LandlordId == id).ExecuteDeleteAsync();
                await _context.Properties.Where(p => p.LandlordId == id).ExecuteDeleteAsync();
                await _context.Landlords.Where(l => l.UserId == id).ExecuteDeleteAsync();
                await _context.Admins.Where(a => a.UserId == id).ExecuteDeleteAsync();

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "User deleted successfully.";
                return RedirectToAction(nameof(ManageUsers));
            }
            catch { await transaction.RollbackAsync(); throw; }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Account");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return RedirectToAction("Login", "Account");
            var model = new UpdateProfileViewModel
            {
                FirstName = user.FirstName,
                SurName = user.SurName,
                Email = user.Email,
                PhoneNo = user.PhoneNo,
                CurrentImagePath = user.ProfileImagePath
            };
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