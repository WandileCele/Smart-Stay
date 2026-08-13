using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
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

        // PasswordHasher<T> works with any plain class - it's not tied
        // to ASP.NET Identity's IdentityUser. It handles salting,
        // hashing, and safe (constant-time) verification for us.
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

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

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            bool passwordValid = false;


            // ============================================================
            // VERIFY PASSWORD
            //
            // Normal case: user.Password holds a proper hash, and
            // VerifyHashedPassword checks it safely.
            //
            // Legacy case: if this user was created before hashing was
            // added, user.Password may still be plaintext. That isn't a
            // valid hash format, so VerifyHashedPassword throws. We
            // catch that, fall back to a one-time plaintext comparison,
            // and if it matches, silently re-hash and save so this
            // account is migrated going forward.
            // ============================================================

            var verificationResult = PasswordVerificationResult.Failed;

            try
            {
                verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            }
            catch (FormatException)
            {
                verificationResult = PasswordVerificationResult.Failed;
            }

            if (verificationResult == PasswordVerificationResult.Success ||
                verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                passwordValid = true;

                if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.Password = _passwordHasher.HashPassword(user, password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            else if (user.Password == password)
            {
                // Legacy plaintext match - migrate to a proper hash now.
                passwordValid = true;

                user.Password = _passwordHasher.HashPassword(user, password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            if (!passwordValid)
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
            if (user.Role == "Tenant")
            {
                return RedirectToAction("Dashboard", "Tenant");
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
                Password = "", // set below, after the hasher has a user instance to work with
                Role = model.Role,
                DateRegistered = DateOnly.FromDateTime(DateTime.Now)
            };

            // Hash the password before it ever touches the database.
            newUser.Password = _passwordHasher.HashPassword(newUser, model.Password);

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