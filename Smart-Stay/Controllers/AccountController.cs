using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using Smart_Stay.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Smart_Stay.Controllers
{
    public class AccountController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDataProtector _protector;
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public AccountController(SmartDbContext context, IEmailService emailService, IDataProtectionProvider dpProvider)
        {
            _context = context;
            _emailService = emailService;
            _protector = dpProvider.CreateProtector("Smart_Stay.EmailVerification.v1");
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
                return RedirectToAction("Dashboard", "Landlord");

            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            if (user.Role == "Tenant")
                return RedirectToAction("Dashboard", "Tenant");

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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(model);
            }

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

            var passwordHash = _passwordHasher.HashPassword(new User(), model.Password);

            var code = Random.Shared.Next(0, 10000).ToString("D4");

            var pending = new PendingRegistrationDto
            {
                FirstName = firstName,
                SurName = surName,
                Email = model.Email,
                PhoneNo = model.PhoneNo,
                PasswordHash = passwordHash,
                Role = model.Role,
                EmploymentStatus = model.EmploymentStatus,
                Code = code,
                ExpiryUtc = DateTime.UtcNow.AddHours(24)
            };

            var json = JsonSerializer.Serialize(pending);
            var protectedJson = _protector.Protect(json);
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedJson));

            var body = $@"
                <h2>Welcome to Smart Stay, {firstName}!</h2>
                <p>Your verification code is:</p>
                <h1 style='letter-spacing:6px;'>{code}</h1>
                <p>Enter this code on the verification page to activate your account. It expires in 24 hours.</p>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, "Your Smart Stay verification code", body);
            }
            catch (Exception)
            {
                ViewBag.EmailFailed = true;
            }

            ViewBag.Token = token;
            ViewBag.PendingEmail = model.Email;
            ViewBag.AwaitingCode = true;

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            PendingRegistrationDto? pending = TryDecodeToken(model.Token);

            if (pending == null)
            {
                ViewBag.CodeError = "Something went wrong. Please register again.";
                return View("VerifyCodeExpired");
            }

            if (pending.ExpiryUtc < DateTime.UtcNow)
            {
                return View("VerifyCodeExpired");
            }

            if (pending.Code != model.Code.Trim())
            {
                ViewBag.CodeError = "Incorrect code. Please try again.";
                ViewBag.Token = model.Token;
                ViewBag.PendingEmail = pending.Email;
                ViewBag.AwaitingCode = true;
                return View("Register", new RegisterViewModel());
            }

            var alreadyExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == pending.Email.ToLower());

            if (alreadyExists)
            {
                ViewBag.CodeError = "This email was already verified or registered.";
                return View("VerifyCodeExpired");
            }

            var newUser = new User
            {
                FirstName = pending.FirstName,
                SurName = pending.SurName,
                Email = pending.Email,
                PhoneNo = pending.PhoneNo,
                Password = pending.PasswordHash,
                Role = pending.Role,
                DateRegistered = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            if (pending.Role == "Landlord")
            {
                _context.Landlords.Add(new Landlord
                {
                    UserId = newUser.UserId,
                    VerificationStatus = "Pending"
                });
            }
            else if (pending.Role == "Tenant")
            {
                _context.Tenants.Add(new Tenant
                {
                    UserId = newUser.UserId,
                    EmploymentStatus = string.IsNullOrWhiteSpace(pending.EmploymentStatus) ? "Employed" : pending.EmploymentStatus
                });
            }
            else if (pending.Role == "Admin")
            {
                _context.Admins.Add(new Admin
                {
                    UserId = newUser.UserId
                });
            }

            await _context.SaveChangesAsync();

            return View("VerifyCodeResult", true);
        }

        private PendingRegistrationDto? TryDecodeToken(string token)
        {
            try
            {
                var protectedJson = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var json = _protector.Unprotect(protectedJson);
                return JsonSerializer.Deserialize<PendingRegistrationDto>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}