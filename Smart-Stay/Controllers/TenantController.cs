using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class TenantController : Controller
    {
        private readonly SmartDbContext _context;

        public TenantController(SmartDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // TENANT DASHBOARD
        // ============================================================

        public async Task<IActionResult> Dashboard()
        {
            var userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var applications = await _context.RentalApplications
                .Include(r => r.Property)
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.ApplicationDate)
                .Select(r => new TenantApplicationViewModel
                {
                    RentalApplicationId =
                        r.RentalApplicationId,

                    PropertyId =
                        r.PropertyId,

                    PropertyTitle =
                        r.Property.Title,

                    Location =
                        r.Property.Location,

                    Price =
                        r.Property.Price,

                    Bedrooms =
                        r.Property.Bedrooms ?? 0,

                    Bathrooms =
                        r.Property.Bathrooms ?? 0,

                    ImagePath =
                        r.Property.ImagePath,

                    ApplicationDate =
                        r.ApplicationDate,

                    ApplicationStatus =
                        r.RentalApplicationStatus
                })
                .ToListAsync();


            var tenantName =
                User.FindFirstValue(ClaimTypes.Name);


            var dashboard =
                new TenantDashboardViewModel
                {
                    TenantName =
                        tenantName ?? "Tenant",

                    TotalApplications =
                        applications.Count,

                    ApprovedApplications =
                        applications.Count(a =>
                            a.ApplicationStatus == "Approved"),

                    PendingApplications =
                        applications.Count(a =>
                            a.ApplicationStatus == "Pending"),

                    RejectedApplications =
                        applications.Count(a =>
                            a.ApplicationStatus == "Rejected"),

                    Applications =
                        applications
                };


            return View(dashboard);
        }


        // ============================================================
        // BROWSE PROPERTIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> BrowseProperties(
            string? search,
            string? location,
            string? price)
        {
            var query = _context.Properties
                .Where(p => p.Status == "Available")
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.Location.Contains(search));
            }


            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(p =>
                    p.Location == location);
            }


            if (!string.IsNullOrWhiteSpace(price))
            {
                switch (price)
                {
                    case "2000-3000":

                        query = query.Where(p =>
                            p.Price >= 2000 &&
                            p.Price <= 3000);

                        break;


                    case "3000-5000":

                        query = query.Where(p =>
                            p.Price >= 3000 &&
                            p.Price <= 5000);

                        break;


                    case "5000+":

                        query = query.Where(p =>
                            p.Price >= 5000);

                        break;
                }
            }


            var properties = await query
                .OrderByDescending(p => p.DateListed)
                .Select(p => new PropertyCardViewModel
                {
                    PropertyID = p.PropertyId,

                    Title = p.Title,

                    Location = p.Location,

                    Price = p.Price,

                    Bedrooms = p.Bedrooms ?? 0,

                    Bathrooms = p.Bathrooms ?? 0,

                    ImagePath =
                        _context.ListingApplications
                            .Where(la =>
                                la.PropertyId == p.PropertyId)
                            .Join(
                                _context.Documents.Where(d =>
                                    d.DocumentType == "Image"),
                                la => la.ListingApplicationId,
                                d => d.ListingApplication,
                                (la, d) => d.DocumentPath
                            )
                            .FirstOrDefault() ?? "",

                    Status = p.Status,

                    ApplicationCount =
                        p.RentalApplications.Count(),

                    AverageRating =
                        p.Reviews.Any()
                            ? p.Reviews.Average(r =>
                                (double)r.Rating)
                            : null,

                    ReviewCount =
                        p.Reviews.Count()
                })
                .ToListAsync();


            ViewBag.Search = search;

            ViewBag.Location = location;

            ViewBag.Price = price;


            return View(properties);
        }


        // ============================================================
        // UPDATE PROFILE - GET
        // ============================================================

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


            var user = await _context.Users
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
                    FirstName = user.FirstName,

                    SurName = user.SurName,

                    Email = user.Email,

                    PhoneNo = user.PhoneNo,

                    CurrentImagePath =
                        user.ProfileImagePath
                };


            return View(model);
        }


        // ============================================================
        // UPDATE PROFILE - POST
        // ============================================================

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


            // PASSWORD VALIDATION

            bool passwordEntered =
                !string.IsNullOrWhiteSpace(
                    model.NewPassword);

            bool confirmPasswordEntered =
                !string.IsNullOrWhiteSpace(
                    model.ConfirmPassword);


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


                if (passwordEntered &&
                    confirmPasswordEntered &&
                    model.NewPassword !=
                    model.ConfirmPassword)
                {
                    ModelState.AddModelError(
                        "ConfirmPassword",
                        "Passwords do not match.");
                }


                if (passwordEntered &&
                    model.NewPassword!.Length < 6)
                {
                    ModelState.AddModelError(
                        "NewPassword",
                        "Password must be at least 6 characters.");
                }
            }


            // CHECK EMAIL

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


            // GET CURRENT USER

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


            // VALIDATION FAILED

            if (!ModelState.IsValid)
            {
                model.CurrentImagePath =
                    user.ProfileImagePath;

                return View(model);
            }


            // UPDATE USER INFORMATION

            user.FirstName =
                model.FirstName.Trim();

            user.SurName =
                model.SurName.Trim();

            user.Email =
                model.Email.Trim();

            user.PhoneNo =
                model.PhoneNo.Trim();


            // UPDATE PASSWORD

            if (!string.IsNullOrWhiteSpace(
                model.NewPassword))
            {
                user.Password =
                    model.NewPassword;
            }


            // IMAGE UPLOAD

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


                var uploadFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "profile-images");


                if (!Directory.Exists(
                    uploadFolder))
                {
                    Directory.CreateDirectory(
                        uploadFolder);
                }


                var fileName =
                    Guid.NewGuid().ToString() +
                    extension;


                var filePath =
                    Path.Combine(
                        uploadFolder,
                        fileName);


                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await model.ProfileImage
                        .CopyToAsync(stream);
                }


                user.ProfileImagePath =
                    "/uploads/profile-images/" +
                    fileName;
            }


            // SAVE DATABASE

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Profile updated successfully!";


            return RedirectToAction(
                "UpdateProfile");
        }
    }
}