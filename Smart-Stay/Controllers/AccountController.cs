using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    public class AccountController : Controller
    {
        private readonly SmartDbContext _context;

        public AccountController(SmartDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.Password != password)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.SurName}"),
                new Claim(ClaimTypes.Role, user.Role ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role == "Landlord")
            {
                return RedirectToAction("Dashboard", "Landlord");
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(model);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ViewBag.EmailTaken = true;
                return View(model);
            }

            var nameParts = model.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var surName = nameParts.Length > 1 ? nameParts[1] : "";

            var newUser = new User
            {
                FirstName = firstName,
                SurName = surName,
                Email = model.Email,
                PhoneNo = model.PhoneNo,
                Password = model.Password,
                Role = model.Role,
                DateRegistered = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            if (model.Role == "Landlord")
            {
                _context.Landlords.Add(new Landlord
                {
                    UserId = newUser.UserId,
                    VerificationStatus = "Pending"
                });
            }
            else if (model.Role == "Tenant")
            {
                _context.Tenants.Add(new Tenant
                {
                    UserId = newUser.UserId,
                    EmploymentStatus = string.IsNullOrWhiteSpace(model.EmploymentStatus) ? "Employed" : model.EmploymentStatus
                });
            }
            else if (model.Role == "Admin")
            {
                _context.Admins.Add(new Admin
                {
                    UserId = newUser.UserId
                });
            }

            await _context.SaveChangesAsync();

            ViewBag.Success = true;
          
            return View(new RegisterViewModel());
        }

    }
}