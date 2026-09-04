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

        public AdminController(SmartDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // DASHBOARD
        // =====================================================

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel();

            int adminUserId = int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);


            model.FirstName = await _context.Users
                .Where(u => u.UserId == adminUserId)
                .Select(u => u.FirstName)
                .FirstOrDefaultAsync() ?? "Admin";


            model.PendingApplications =
                await _context.ListingApplications
                    .CountAsync(a =>
                        a.ApplicationStatus == "Pending");


            model.ApprovedApplications =
                await _context.ListingApplications
                    .CountAsync(a =>
                        a.ApplicationStatus == "Approved");


            model.RejectedApplications =
                await _context.ListingApplications
                    .CountAsync(a =>
                        a.ApplicationStatus == "Rejected");


            model.Applications =
                await _context.ListingApplications

                    .Include(a => a.Property)

                    .Include(a => a.Landlord)
                        .ThenInclude(l => l.User)

                    .Where(a =>
                        a.ApplicationStatus == "Pending")

                    .Select(a =>
                        new ListingApplicationCardViewModel
                        {
                            ListingApplicationId =
                                a.ListingApplicationId,

                            PropertyId =
                                a.PropertyId ?? 0,

                            PropertyTitle =
                                a.Property != null
                                    ? a.Property.Title
                                    : "",

                            LandlordName =
                                a.Landlord != null
                                    ? a.Landlord.User.FirstName +
                                      " " +
                                      a.Landlord.User.SurName
                                    : "",

                            Location =
                                a.Property != null
                                    ? a.Property.Location
                                    : "",

                            Price =
                                a.Property != null
                                    ? a.Property.Price
                                    : 0,

                            PropertyType =
                                a.Property != null
                                    ? a.Property.PropertyType
                                    : "",

                            ApplicationDate =
                                a.ApplicationDate,

                            ApplicationStatus =
                                a.ApplicationStatus
                        })

                    .ToListAsync();


            return View(model);
        }


        // =====================================================
        // APPROVE APPLICATION
        // =====================================================

        public async Task<IActionResult> Approve(int id)
        {
            var application =
                await _context.ListingApplications
                    .Include(a => a.Property)
                    .FirstOrDefaultAsync(a =>
                        a.ListingApplicationId == id);


            if (application == null)
            {
                return NotFound();
            }


            application.ApplicationStatus = "Approved";


            if (application.Property != null)
            {
                application.Property.Status = "Approved";
            }


            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Dashboard));
        }


        // =====================================================
        // REJECT APPLICATION
        // =====================================================

        public async Task<IActionResult> Reject(int id)
        {
            var application =
                await _context.ListingApplications
                    .Include(a => a.Property)
                    .FirstOrDefaultAsync(a =>
                        a.ListingApplicationId == id);


            if (application == null)
            {
                return NotFound();
            }


            application.ApplicationStatus = "Rejected";


            if (application.Property != null)
            {
                application.Property.Status = "Rejected";
            }


            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Dashboard));
        }


        // =====================================================
        // APPLICATION DETAILS
        // =====================================================

        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application =
                await _context.ListingApplications

                    .Include(a => a.Property)

                    .Include(a => a.Landlord)
                        .ThenInclude(l => l.User)

                    .FirstOrDefaultAsync(a =>
                        a.ListingApplicationId == id);


            if (application == null ||
                application.Property == null)
            {
                return NotFound();
            }


            var documents =
                await _context.Documents

                    .Where(d =>
                        d.ListingApplication == id)

                    .ToListAsync();


            var model =
                new ListingApplicationReviewViewModel
                {
                    ListingApplicationId =
                        application.ListingApplicationId,

                    ApplicationStatus =
                        application.ApplicationStatus,

                    ApplicationDate =
                        application.ApplicationDate,


                    PropertyId =
                        application.Property.PropertyId,

                    Title =
                        application.Property.Title,

                    Description =
                        application.Property.Description,

                    Location =
                        application.Property.Location,

                    Price =
                        application.Property.Price,

                    PropertyType =
                        application.Property.PropertyType,

                    Bedrooms =
                        application.Property.Bedrooms,

                    Bathrooms =
                        application.Property.Bathrooms,


                    LandlordName =
                        application.Landlord != null
                            ? application.Landlord.User.FirstName +
                              " " +
                              application.Landlord.User.SurName
                            : "",


                    AffidavitPath =
                        documents

                            .Where(d =>
                                d.DocumentType == "Affidavit")

                            .Select(d =>
                                d.DocumentPath)

                            .FirstOrDefault(),


                    ImagePaths =
                        documents

                            .Where(d =>
                                d.DocumentType == "Image")

                            .Select(d =>
                                d.DocumentPath)

                            .ToList()
                };


            return View(model);
        }


        // =====================================================
        // MANAGE LANDLORDS
        // =====================================================

        public async Task<IActionResult> ManageLandlords()
        {
            var landlords =
                await _context.Landlords

                    .Include(l => l.User)

                    .Include(l => l.Properties)

                    .ToListAsync();


            return View(landlords);
        }


        // =====================================================
        // DELETE LANDLORD
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLandlord(int id)
        {
            var landlord =
                await _context.Landlords

                    .Include(l => l.User)

                    .FirstOrDefaultAsync(l =>
                        l.UserId == id);


            if (landlord == null)
            {
                return NotFound();
            }


            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                // GET LANDLORD PROPERTIES

                var properties =
                    await _context.Properties

                        .Where(p =>
                            p.LandlordId == id)

                        .ToListAsync();


                var propertyIds =
                    properties

                        .Select(p =>
                            p.PropertyId)

                        .ToList();


                // DELETE REVIEWS

                if (propertyIds.Any())
                {
                    var reviews =
                        await _context.Reviews

                            .Where(r =>
                                propertyIds.Contains(
                                    r.PropertyId))

                            .ToListAsync();


                    _context.Reviews.RemoveRange(reviews);
                }


                // GET LISTING APPLICATIONS

                var listingApplications =
                    await _context.ListingApplications

                        .Where(a =>
                            a.LandlordId == id)

                        .ToListAsync();


                var listingApplicationIds =
                    listingApplications

                        .Select(a =>
                            a.ListingApplicationId)

                        .ToList();


                // DELETE LISTING DOCUMENTS

                if (listingApplicationIds.Any())
                {
                    var listingDocuments =
                        await _context.Documents

                            .Where(d =>
                                d.ListingApplication != null &&
                                listingApplicationIds.Contains(
                                    d.ListingApplication.Value))

                            .ToListAsync();


                    _context.Documents.RemoveRange(
                        listingDocuments);
                }


                _context.ListingApplications
                    .RemoveRange(listingApplications);


                // GET RENTAL APPLICATIONS

                var rentalApplications =
                    await _context.RentalApplications

                        .Where(r =>
                            r.LandlordId == id)

                        .ToListAsync();


                var rentalApplicationIds =
                    rentalApplications

                        .Select(r =>
                            r.RentalApplicationId)

                        .ToList();


                // DELETE RENTAL DOCUMENTS

                if (rentalApplicationIds.Any())
                {
                    var rentalDocuments =
                        await _context.Documents

                            .Where(d =>
                                d.RentalApplicationId.HasValue &&
                                rentalApplicationIds.Contains(
                                    d.RentalApplicationId.Value))

                            .ToListAsync();


                    _context.Documents.RemoveRange(
                        rentalDocuments);
                }


                _context.RentalApplications
                    .RemoveRange(rentalApplications);


                // DELETE PROPERTIES

                _context.Properties
                    .RemoveRange(properties);


                // DELETE LANDLORD

                _context.Landlords
                    .Remove(landlord);


                // DELETE USER

                _context.Users
                    .Remove(landlord.User);


                // SAVE DATABASE

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                return RedirectToAction(
                    nameof(ManageLandlords));
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }


        // =====================================================
        // GET UPDATE PROFILE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userIdString =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            if (!int.TryParse(
                userIdString,
                out int userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var user =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var model =
                new UpdateProfileViewModel
                {
                    FirstName =
                        user.FirstName,

                    SurName =
                        user.SurName,

                    Email =
                        user.Email,

                    PhoneNo =
                        user.PhoneNo,

                    CurrentImagePath =
                        user.ProfileImagePath
                };


            return View(model);
        }


        // =====================================================
        // POST UPDATE PROFILE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileViewModel model)
        {
            var userIdString =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            if (!int.TryParse(
                userIdString,
                out int userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // =================================================
            // GET CURRENT USER
            // =================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == userId);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // =================================================
            // CHECK PASSWORD
            // =================================================

            bool passwordEntered =
                !string.IsNullOrWhiteSpace(
                    model.NewPassword);

            bool confirmPasswordEntered =
                !string.IsNullOrWhiteSpace(
                    model.ConfirmPassword);


            // If one password field is entered,
            // both must be entered

            if (passwordEntered ||
                confirmPasswordEntered)
            {
                if (!passwordEntered)
                {
                    ModelState.AddModelError(
                        "NewPassword",
                        "Please enter a new password.");
                }


                if (!confirmPasswordEntered)
                {
                    ModelState.AddModelError(
                        "ConfirmPassword",
                        "Please confirm your password.");
                }


                // Passwords must match

                if (passwordEntered &&
                    confirmPasswordEntered &&
                    model.NewPassword !=
                    model.ConfirmPassword)
                {
                    ModelState.AddModelError(
                        "ConfirmPassword",
                        "Passwords do not match.");
                }


                if (passwordEntered)
                {
                    string password =
                        model.NewPassword!;


                    // At least 8 characters

                    if (password.Length < 8)
                    {
                        ModelState.AddModelError(
                            "NewPassword",
                            "Password must be at least 8 characters.");
                    }


                    // At least one uppercase letter

                    if (!password.Any(char.IsUpper))
                    {
                        ModelState.AddModelError(
                            "NewPassword",
                            "Password must contain at least 1 uppercase letter.");
                    }


                    // At least one number

                    if (!password.Any(char.IsDigit))
                    {
                        ModelState.AddModelError(
                            "NewPassword",
                            "Password must contain at least 1 number.");
                    }


                    // At least one special character

                    if (!password.Any(
                        c => !char.IsLetterOrDigit(c)))
                    {
                        ModelState.AddModelError(
                            "NewPassword",
                            "Password must contain at least 1 special character.");
                    }
                }
            }


            // =================================================
            // CHECK IF EMAIL ALREADY EXISTS
            // =================================================

            var emailExists =
                await _context.Users.AnyAsync(u =>
                    u.Email == model.Email &&
                    u.UserId != userId);


            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already being used.");
            }


            // =================================================
            // VALIDATION FAILED
            // =================================================

            if (!ModelState.IsValid)
            {
                model.CurrentImagePath =
                    user.ProfileImagePath;

                return View(model);
            }


            // =================================================
            // UPDATE USER INFORMATION
            // =================================================

            user.FirstName =
                model.FirstName.Trim();

            user.SurName =
                model.SurName.Trim();

            user.Email =
                model.Email.Trim();

            user.PhoneNo =
                model.PhoneNo.Trim();


            // =================================================
            // UPDATE PASSWORD
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                model.NewPassword))
            {
                user.Password =
                    model.NewPassword;
            }


            // =================================================
            // PROFILE IMAGE UPLOAD
            // =================================================

            if (model.ProfileImage != null &&
                model.ProfileImage.Length > 0)
            {
                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".gif"
                    };


                var extension =
                    Path.GetExtension(
                        model.ProfileImage.FileName)
                    .ToLowerInvariant();


                // CHECK FILE TYPE

                if (!allowedExtensions.Contains(
                    extension))
                {
                    ModelState.AddModelError(
                        "ProfileImage",
                        "Only JPG, JPEG, PNG and GIF files are allowed.");

                    model.CurrentImagePath =
                        user.ProfileImagePath;

                    return View(model);
                }


                // CHECK FILE SIZE

                if (model.ProfileImage.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "ProfileImage",
                        "Image must be smaller than 5MB.");

                    model.CurrentImagePath =
                        user.ProfileImagePath;

                    return View(model);
                }


                // CREATE FOLDER

                var uploadFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "profile-images");


                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(
                        uploadFolder);
                }


                // CREATE UNIQUE FILE NAME

                var fileName =
                    Guid.NewGuid().ToString() +
                    extension;


                var filePath =
                    Path.Combine(
                        uploadFolder,
                        fileName);


                // SAVE IMAGE

                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await model.ProfileImage
                        .CopyToAsync(stream);
                }


                // SAVE IMAGE PATH TO DATABASE

                user.ProfileImagePath =
                    "/uploads/profile-images/" +
                    fileName;
            }


            // =================================================
            // SAVE ALL CHANGES TO DATABASE
            // =================================================

            await _context.SaveChangesAsync();


            // =================================================
            // SUCCESS MESSAGE
            // =================================================

            TempData["SuccessMessage"] =
                "Profile updated successfully!";


            // IMPORTANT:
            // Redirect back to Admin UpdateProfile

            return RedirectToAction(
                nameof(UpdateProfile));
        }
    }
}