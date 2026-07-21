using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;

namespace Smart_Stay.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SmartDbContext _context;

        public HomeController(ILogger<HomeController> logger, SmartDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var properties = await _context.Properties
                .Where(p => p.Status == "Available")
                .OrderByDescending(p => p.DateListed)
                .Select(p => new PropertyCardViewModel
                {
                    PropertyID = p.PropertyId,
                    Title = p.Title,
                    Location = p.Location,
                    Price = p.Price,
                    Bedrooms = p.Bedrooms ?? 0,
                    Bathrooms = p.Bathrooms ?? 0,
                    ImagePath = _context.Documents
                        .Where(d => d.ListingApplicationNavigation.PropertyId == p.PropertyId
                                    && d.DocumentType == "Image")
                        .OrderByDescending(d => d.UploadDate)
                        .Select(d => d.DocumentPath)
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
    }
}