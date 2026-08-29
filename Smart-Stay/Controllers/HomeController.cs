using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Services;
using Smart_Stay.Models;



namespace Smart_Stay.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;
        private readonly SmartDbContext _context;
      
        public HomeController(ILogger<HomeController> logger, SmartDbContext context,IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            

            var properties = await _context.Properties
           .Where(p => p.Status == "Available")
           .OrderByDescending(p => p.DateListed)
           .Take(3)
           .Select(p => new PropertyCardViewModel
        {
          PropertyID = p.PropertyId,
           Title = p.Title,
           Location = p.Location,
           Price = p.Price,
          Bedrooms = p.Bedrooms ?? 0,
          Bathrooms = p.Bathrooms ?? 0,

       AverageRating = p.Reviews.Any()
        ? p.Reviews.Average(r => (double)r.Rating)
        : null,

       ReviewCount = p.Reviews.Count(),

       ImagePath = _context.ListingApplications
        .Where(la => la.PropertyId == p.PropertyId)
        .Join(
            _context.Documents.Where(d => d.DocumentType == "Image"),
            la => la.ListingApplicationId,
            d => d.ListingApplication,
            (la, d) => d.DocumentPath
        )
        .FirstOrDefault()
   })
    .ToListAsync();

            return View(properties);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public IActionResult About(string? returnUrl)
        {
            string? candidateReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();

            string safeReturnUrl = (!string.IsNullOrWhiteSpace(candidateReturnUrl)
                                     && Url.IsLocalUrl(candidateReturnUrl))
                ? candidateReturnUrl
                : Url.Action("Index", "Home")!;

            ViewBag.ReturnUrl = safeReturnUrl;
            return View();
        }
        [HttpGet]
        [HttpGet]
        public IActionResult Contact(string? returnUrl)
        {
            string? candidateReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();

            string safeReturnUrl = (!string.IsNullOrWhiteSpace(candidateReturnUrl)
                                     && Url.IsLocalUrl(candidateReturnUrl))
                ? candidateReturnUrl
                : Url.Action("Index", "Home")!;

            ViewBag.ReturnUrl = safeReturnUrl;

            return View();
        }
        [HttpGet]
        public IActionResult HowItWorks()
        {
            return View();
        }
        [HttpGet]
        public IActionResult FAQ()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Support(string? returnUrl)
        {
            string? candidateReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();

            string safeReturnUrl = (!string.IsNullOrWhiteSpace(candidateReturnUrl)
                                     && Url.IsLocalUrl(candidateReturnUrl))
                ? candidateReturnUrl
                : Url.Action("Index", "Home")!;

            ViewBag.ReturnUrl = safeReturnUrl;

            return View("support");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailBody = $@"
        <h2>New Contact Us Message - Smart Stay</h2>

        <p><strong>Name:</strong> {model.Name}</p>
        <p><strong>Email:</strong> {model.Email}</p>
        <p><strong>Subject:</strong> {model.Subject}</p>

        <hr />

        <h3>Message</h3>
        <p>{model.Message}</p>

        <hr />

        <p>This message was sent from the Smart Stay Contact Us form.</p>
    ";

            try
            {
                await _emailService.SendEmailAsync(
                    "smartstay729@gmail.com",
                    $"Contact Us: {model.Subject}",
                    emailBody
                );

                TempData["ContactSuccess"] =
                    "Your message has been sent successfully. We will get back to you soon.";

                return RedirectToAction("Contact");
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "Unable to send your message right now. Please try again later."
                );

                return View(model);
            }
        }
    }
}