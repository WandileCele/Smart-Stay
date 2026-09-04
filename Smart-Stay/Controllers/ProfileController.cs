using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Tenant,Landlord")]
    public class ProfileController : Controller
    {
        private readonly SmartDbContext _context;

        public ProfileController(SmartDbContext context)
        {
            _context = context;
        }


        // ============================
        // UPDATE PROFILE - GET
        // ============================

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new UpdateProfileViewModel
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                SurName = user.SurName,
                Email = user.Email,
                PhoneNo = user.PhoneNo
            };

            return View(model);
        }


        // ============================
        // UPDATE PROFILE - POST
        // ============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Update basic information

            user.FirstName = model.FirstName;
            user.SurName = model.SurName;
            user.Email = model.Email;
            user.PhoneNo = model.PhoneNo;

            // Only change password if one was entered

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.Password = model.NewPassword;
            }

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(UpdateProfile));
        }
    }
}