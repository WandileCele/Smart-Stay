using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class LandlordController : Controller
    {
        private readonly SmartDbContext _context;

        public LandlordController(SmartDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // DASHBOARD
        // ============================================================

        public async Task<IActionResult> Dashboard()
        {
            int landlordId =
                int.Parse(
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier)!);

            var landlord = await _context.Landlords
                .Include(l => l.User)
                .FirstOrDefaultAsync(
                    l => l.UserId == landlordId);

            var model = new LandlordDashboardViewModel
            {
                FirstName = landlord.User.FirstName
            };

            model.TotalProperties =
                await _context.Properties
                .CountAsync(p =>
                    p.LandlordId == landlordId);


            model.AvailableProperties =
                await _context.Properties
                .CountAsync(p =>
                    p.LandlordId == landlordId &&
                    p.Status == "Available");


            model.TotalApplications =
                await _context.RentalApplications
                .CountAsync(r =>
                    r.LandlordId == landlordId);


            model.Properties =
                await _context.Properties
                .Where(p =>
                    p.LandlordId == landlordId)
                .Select(p =>
                    new PropertyCardViewModel
                    {
                        PropertyID = p.PropertyId,

                        Title = p.Title,

                        Location = p.Location,

                        Price = p.Price,

                        Bedrooms =
                            p.Bedrooms ?? 0,

                        Bathrooms =
                            p.Bathrooms ?? 0,

                        ImagePath =
                            p.ImagePath,

                        Status =
                            p.Status,

                        ApplicationCount =
                            p.RentalApplications.Count()
                    })
                .ToListAsync();


            return View(model);
        }


        // ============================================================
        // UPDATE PROFILE - GET
        // THIS LOADS THE CURRENT USER INFORMATION
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


        // ============================================================
        // UPDATE PROFILE - POST
        // THIS SAVES CHANGES TO THE DATABASE
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


            // ========================================================
            // GET CURRENT USER
            // ========================================================

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


            // ========================================================
            // CHECK EMAIL
            // ========================================================

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


            // ========================================================
            // VALIDATION FAILED
            // ========================================================

            if (!ModelState.IsValid)
            {
                model.CurrentImagePath =
                    user.ProfileImagePath;

                return View(model);
            }


            // ========================================================
            // UPDATE USER INFORMATION
            // ========================================================

            user.FirstName =
                model.FirstName.Trim();

            user.SurName =
                model.SurName.Trim();

            user.Email =
                model.Email.Trim();

            user.PhoneNo =
                model.PhoneNo.Trim();


            // ========================================================
            // UPDATE PASSWORD
            // Only update if the user entered a password
            // ========================================================

            if (!string.IsNullOrWhiteSpace(
                model.NewPassword))
            {
                user.Password =
                    model.NewPassword;
            }


            // ========================================================
            // IMAGE UPLOAD
            // ========================================================

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


                // ----------------------------------------------------
                // CHECK IMAGE TYPE
                // ----------------------------------------------------

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


                // ----------------------------------------------------
                // CHECK IMAGE SIZE
                // ----------------------------------------------------

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


                // ----------------------------------------------------
                // CREATE UPLOAD FOLDER
                // ----------------------------------------------------

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


                // ----------------------------------------------------
                // CREATE UNIQUE FILE NAME
                // ----------------------------------------------------

                var fileName =
                    Guid.NewGuid().ToString() +
                    extension;


                var filePath =
                    Path.Combine(
                        uploadFolder,
                        fileName);


                // ----------------------------------------------------
                // SAVE IMAGE
                // ----------------------------------------------------

                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await model.ProfileImage
                        .CopyToAsync(stream);
                }


                // ----------------------------------------------------
                // SAVE IMAGE PATH IN DATABASE
                // ----------------------------------------------------

                user.ProfileImagePath =
                    "/uploads/profile-images/" +
                    fileName;
            }


            // ========================================================
            // SAVE ALL CHANGES TO DATABASE
            // ========================================================

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Profile updated successfully!";


            // ========================================================
            // REDIRECT BACK TO PROFILE
            // ========================================================

            return RedirectToAction(
                nameof(UpdateProfile));
        }
    }
}