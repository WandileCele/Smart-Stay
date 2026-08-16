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
                    RentalApplicationId = r.RentalApplicationId,

                    PropertyId = r.PropertyId,

                    PropertyTitle = r.Property.Title,

                    Location = r.Property.Location,

                    Price = r.Property.Price,

                    Bedrooms = r.Property.Bedrooms ?? 0,

                    Bathrooms = r.Property.Bathrooms ?? 0,

                    ImagePath = r.Property.ImagePath,

                    ApplicationDate = r.ApplicationDate,

                    ApplicationStatus = r.RentalApplicationStatus
                })
                .ToListAsync();


            var tenantName =
                User.FindFirstValue(ClaimTypes.Name);


            var dashboard = new TenantDashboardViewModel
            {
                TenantName = tenantName ?? "Tenant",

                TotalApplications = applications.Count,

                ApprovedApplications = applications.Count(a =>
                    a.ApplicationStatus == "Approved"),

                PendingApplications = applications.Count(a =>
                    a.ApplicationStatus == "Pending"),

                RejectedApplications = applications.Count(a =>
                    a.ApplicationStatus == "Rejected"),

                Applications = applications
            };


            return View(dashboard);
        }


        // ============================================================
        // BROWSE ALL AVAILABLE PROPERTIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> BrowseProperties(
            string? search,
            string? location,
            string? price)
        {
            // Start with available properties only
            var query = _context.Properties
                .Where(p => p.Status == "Available")
                .AsQueryable();


            // ========================================================
            // SEARCH
            // ========================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.Location.Contains(search));
            }


            // ========================================================
            // LOCATION FILTER
            // ========================================================

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(p =>
                    p.Location == location);
            }


            // ========================================================
            // PRICE FILTER
            // ========================================================

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

                    ImagePath = _context.ListingApplications
                        .Where(la => la.PropertyId == p.PropertyId)
                        .Join(
                            _context.Documents.Where(d => d.DocumentType == "Image"),
                            la => la.ListingApplicationId,
                            d => d.ListingApplication,
                            (la, d) => d.DocumentPath
                        )
                        .FirstOrDefault() ?? "",

                    Status = p.Status,

                    ApplicationCount = p.RentalApplications.Count(),

                    AverageRating = p.Reviews.Any()
                        ? p.Reviews.Average(r => (double)r.Rating)
                        : null,

                    ReviewCount = p.Reviews.Count()
                })
                .ToListAsync();


            // ========================================================
            // SEND CURRENT FILTER VALUES BACK TO VIEW
            // ========================================================

            ViewBag.Search = search;

            ViewBag.Location = location;

            ViewBag.Price = price;


            return View(properties);
        }
    }
}