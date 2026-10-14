
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Smart_Stay.Data;
using Smart_Stay.Models;
using Smart_Stay.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Smart_Stay.Controllers
{
    public class AccountController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDataProtector _protector;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountController> _logger;

        private readonly PasswordHasher<User> _passwordHasher =
            new PasswordHasher<User>();

        public AccountController(
            SmartDbContext context,
            IEmailService emailService,
            IDataProtectionProvider dpProvider,
            IMemoryCache cache,
            ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;

            _protector = dpProvider.CreateProtector(
                "Smart_Stay.EmailVerification.v1");
        }

        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            string? returnUrl = null)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            if (user.IsSuspended)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been suspended. Please contact Smart Stay support.");

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            bool passwordValid = false;
            var verificationResult = PasswordVerificationResult.Failed;

            try
            {
                verificationResult = _passwordHasher
                    .VerifyHashedPassword(user, user.Password, password);
            }
            catch (FormatException)
            {
                verificationResult = PasswordVerificationResult.Failed;
            }

            if (verificationResult == PasswordVerificationResult.Success ||
                verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                passwordValid = true;

                if (verificationResult ==
                    PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.Password = _passwordHasher.HashPassword(user, password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            else if (user.Password == password)
            {
                // Migrate older plaintext passwords to hashed passwords.
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
                new Claim(ClaimTypes.Name,
                    $"{user.FirstName} {user.SurName}"),
                new Claim(ClaimTypes.Role, user.Role ?? "")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "Landlord")
                return RedirectToAction("Dashboard", "Landlord");

            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            if (user.Role == "Tenant")
                return RedirectToAction("Dashboard", "Tenant");

            return RedirectToAction("Index", "Home");
        }

        // ==================== FORGOT PASSWORD ====================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Message"] = "Please enter your email address.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // Use the same message whether the account exists or not.
            TempData["Message"] =
                "If an account exists with that email, a verification code will be sent.";

            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Generate a random six-digit code.
            var code = RandomNumberGenerator.GetInt32(100000, 1000000)
                .ToString();

            // Use a random flow ID so the code is not exposed in the URL.
            var flowId = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));

            var resetState = new PasswordResetState
            {
                UserId = user.UserId,
                Email = user.Email,
                Code = code,
                ExpiryUtc = DateTime.UtcNow.AddMinutes(10),
                Attempts = 0
            };

            _cache.Set(
                "PasswordResetCode:" + flowId,
                resetState,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = resetState.ExpiryUtc
                });

            var safeCode =
                System.Net.WebUtility.HtmlEncode(code);

            var body = $@"
                <h2>Smart Stay Password Reset</h2>
                <p>Your password verification code is:</p>
                <h1 style='letter-spacing:8px;'>{safeCode}</h1>
                <p>This code expires in 10 minutes.</p>
                <p>If you did not request this code, ignore this email.</p>";

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Your Smart Stay Password Reset Code",
                    body);
            }
            catch (Exception ex)
            {
                _cache.Remove("PasswordResetCode:" + flowId);

                _logger.LogError(
                    ex,
                    "Failed to send password reset code.");

                TempData["Message"] =
                    "We could not send the verification email. Please try again later.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            TempData.Remove("Message");

            return RedirectToAction(
                nameof(VerifyResetCode),
                new { flowId });
        }

        // ==================== VERIFY RESET CODE ====================

        [HttpGet]
        public IActionResult VerifyResetCode(string? flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId) ||
                !_cache.TryGetValue(
                    "PasswordResetCode:" + flowId,
                    out PasswordResetState? state) ||
                state == null ||
                state.ExpiryUtc <= DateTime.UtcNow)
            {
                if (!string.IsNullOrWhiteSpace(flowId))
                    _cache.Remove("PasswordResetCode:" + flowId);

                TempData["Message"] =
                    "Your verification code has expired. Please request a new one.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            ViewBag.FlowId = flowId;
            ViewBag.Email = MaskEmail(state.Email);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyResetCode(
            string? flowId,
            string? code)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                ModelState.AddModelError("", "Please request a new code.");
                return View();
            }

            var cacheKey = "PasswordResetCode:" + flowId;

            if (!_cache.TryGetValue(
                    cacheKey,
                    out PasswordResetState? state) ||
                state == null ||
                state.ExpiryUtc <= DateTime.UtcNow)
            {
                _cache.Remove(cacheKey);

                ModelState.AddModelError(
                    "",
                    "Your code has expired. Please request a new one.");

                return View();
            }

            if (state.Attempts >= 5)
            {
                _cache.Remove(cacheKey);

                ModelState.AddModelError(
                    "",
                    "Too many incorrect attempts. Please request a new code.");

                return View();
            }

            state.Attempts++;

            if (string.IsNullOrWhiteSpace(code) ||
                !System.Security.Cryptography.CryptographicOperations
                    .FixedTimeEquals(
                        Encoding.UTF8.GetBytes(state.Code),
                        Encoding.UTF8.GetBytes(code.Trim().Length == 6
                            ? code.Trim()
                            : "000000")))
            {
                if (state.Attempts >= 5)
                {
                    _cache.Remove(cacheKey);

                    ModelState.AddModelError(
                        "",
                        "Too many incorrect attempts. Please request a new code.");
                }
                else
                {
                    _cache.Set(
                        cacheKey,
                        state,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = state.ExpiryUtc
                        });

                    ModelState.AddModelError(
                        "",
                        $"Incorrect code. You have {5 - state.Attempts} attempts remaining.");
                }

                ViewBag.FlowId = flowId;
                ViewBag.Email = MaskEmail(state.Email);

                return View();
            }

            // The code is valid. Remove it so it cannot be used again.
            _cache.Remove(cacheKey);

            // Create a separate, short-lived permission to reset the password.
            var grant = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));

            _cache.Set(
                "PasswordResetGrant:" + grant,
                new PasswordResetGrant
                {
                    UserId = state.UserId,
                    Email = state.Email
                },
                TimeSpan.FromMinutes(10));

            return RedirectToAction(
                nameof(ResetPassword),
                new { grant });
        }

        // ==================== RESET PASSWORD ====================

        [HttpGet]
        public IActionResult ResetPassword(string? grant)
        {
            if (!HasValidResetGrant(grant))
            {
                return View("ResetPasswordExpired");
            }

            ViewBag.Grant = grant;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string? grant,
            string? password,
            string? confirmPassword)
        {
            if (!HasValidResetGrant(grant))
            {
                return View("ResetPasswordExpired");
            }

            if (string.IsNullOrWhiteSpace(password) ||
                password.Length < 8)
            {
                ModelState.AddModelError(
                    "password",
                    "Password must contain at least 8 characters.");
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Grant = grant;
                return View();
            }

            var grantKey = "PasswordResetGrant:" + grant;

            if (!_cache.TryGetValue(
                    grantKey,
                    out PasswordResetGrant? resetGrant) ||
                resetGrant == null)
            {
                return View("ResetPasswordExpired");
            }

            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.UserId == resetGrant.UserId &&
                     u.Email == resetGrant.Email);

            if (user == null)
            {
                _cache.Remove(grantKey);
                return View("ResetPasswordExpired");
            }

            user.Password = _passwordHasher.HashPassword(
                user,
                password!);

            await _context.SaveChangesAsync();

            // Make the reset permission single-use.
            _cache.Remove(grantKey);

            TempData["Message"] =
                "Your password has been reset successfully. You can now log in.";

            return RedirectToAction(nameof(Login));
        }

        private bool HasValidResetGrant(string? grant)
        {
            return !string.IsNullOrWhiteSpace(grant) &&
                   _cache.TryGetValue(
                       "PasswordResetGrant:" + grant,
                       out PasswordResetGrant? resetGrant) &&
                   resetGrant != null;
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');

            if (parts.Length != 2)
                return "your registered email";

            var name = parts[0];

            var visible = name.Length <= 2
                ? name[0].ToString()
                : name[..2];

            return visible + "***@" + parts[1];
        }

        private sealed class PasswordResetState
        {
            public int UserId { get; set; }
            public string Email { get; set; } = "";
            public string Code { get; set; } = "";
            public DateTime ExpiryUtc { get; set; }
            public int Attempts { get; set; }
        }

        private sealed class PasswordResetGrant
        {
            public int UserId { get; set; }
            public string Email { get; set; } = "";
        }

        // ==================== LOGOUT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        // ==================== ACCESS DENIED ====================

        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==================== REGISTER ====================

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
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(model);
            }

            var normalizedEmail =
                model.Email.Trim().ToLowerInvariant();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (existingUser != null)
            {
                ViewBag.EmailTaken = true;
                return View(model);
            }

            var nameParts = model.FullName.Trim().Split(' ', 2);

            var firstName = nameParts[0];

            var surName = nameParts.Length > 1
                ? nameParts[1]
                : "";

            var passwordHash = _passwordHasher.HashPassword(
                new User(),
                model.Password);

            var code = Random.Shared.Next(0, 10000).ToString("D4");

            var pending = new PendingRegistrationDto
            {
                FirstName = firstName,
                SurName = surName,
                Email = normalizedEmail,
                PhoneNo = model.PhoneNo,
                PasswordHash = passwordHash,
                Role = model.Role,
                EmploymentStatus = model.EmploymentStatus,
                Code = code,
                ExpiryUtc = DateTime.UtcNow.AddHours(24)
            };

            var json = JsonSerializer.Serialize(pending);
            var protectedJson = _protector.Protect(json);

            var token = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(protectedJson));

            var safeFirstName =
                System.Net.WebUtility.HtmlEncode(firstName);

            var body = $@"
                <h2>Welcome to Smart Stay, {safeFirstName}!</h2>
                <p>Your verification code is:</p>
                <h1 style='letter-spacing:6px;'>{code}</h1>
                <p>Enter this code on the verification page to activate your account. It expires in 24 hours.</p>";

            _ = SendVerificationEmailInBackground(
                normalizedEmail,
                body);

            ViewBag.Token = token;
            ViewBag.PendingEmail = normalizedEmail;
            ViewBag.AwaitingCode = true;

            return View(new RegisterViewModel());
        }

        private async Task SendVerificationEmailInBackground(
            string toEmail,
            string body)
        {
            try
            {
                await _emailService.SendEmailAsync(
                    toEmail,
                    "Your Smart Stay verification code",
                    body);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send verification email to {Email}",
                    toEmail);
            }
        }

        // ==================== VERIFY REGISTRATION ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(
            VerifyCodeViewModel model)
        {
            PendingRegistrationDto? pending = TryDecodeToken(model.Token);

            if (pending == null)
            {
                ViewBag.CodeError =
                    "Something went wrong. Please register again.";

                return View("VerifyCodeExpired");
            }

            if (pending.ExpiryUtc < DateTime.UtcNow)
                return View("VerifyCodeExpired");

            if (pending.Code != model.Code.Trim())
            {
                ViewBag.CodeError = "Incorrect code. Please try again.";
                ViewBag.Token = model.Token;
                ViewBag.PendingEmail = pending.Email;
                ViewBag.AwaitingCode = true;

                return View("Register", new RegisterViewModel());
            }

            var alreadyExists = await _context.Users
                .AnyAsync(u => u.Email == pending.Email);

            if (alreadyExists)
            {
                ViewBag.CodeError =
                    "This email was already verified or registered.";

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
                    EmploymentStatus =
                        string.IsNullOrWhiteSpace(pending.EmploymentStatus)
                            ? "Employed"
                            : pending.EmploymentStatus
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
                var protectedJson = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(token));

                var json = _protector.Unprotect(protectedJson);

                return JsonSerializer
                    .Deserialize<PendingRegistrationDto>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
