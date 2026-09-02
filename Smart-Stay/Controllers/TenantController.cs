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

                    ApplicationStatus = r.RentalApplicationStatus,

                    LeaseStartDate = r.LeaseStartDate,

                    LeaseEndDate = r.LeaseEndDate,


                    // ====================================================
                    // RATING ELIGIBILITY
                    // Tenant must be approved and today must be AFTER
                    // the LeaseStartDate.
                    // ====================================================

                    CanRate = r.RentalApplicationStatus == "Approved"
                              && r.LeaseStartDate.HasValue
                              && r.LeaseStartDate.Value
                                  < DateOnly.FromDateTime(DateTime.Today),


                    // ====================================================
                    // CHECK WHETHER THIS TENANT ALREADY RATED
                    // THIS PROPERTY
                    // ====================================================

                    HasRated = r.Property.Reviews
                        .Any(review => review.TenantId == tenantId)
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
        // RATE PROPERTY - SHOW RATING PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> RateProperty(int applicationId)
        {
            var userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }


            // Find the application belonging to the logged-in tenant
            var application = await _context.RentalApplications
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r =>
                    r.RentalApplicationId == applicationId &&
                    r.TenantId == tenantId);


            if (application == null)
            {
                return NotFound();
            }


            // Only approved applications can rate
            if (application.RentalApplicationStatus != "Approved")
            {
                return Forbid();
            }


            // Tenant must be past the lease start date
            if (!application.LeaseStartDate.HasValue ||
                application.LeaseStartDate.Value >=
                DateOnly.FromDateTime(DateTime.Today))
            {
                TempData["RatingError"] =
                    "You can only rate the property after your lease has started.";

                return RedirectToAction("Dashboard");
            }


            // Check if this tenant has already rated this property
            var alreadyRated = await _context.Reviews
                .AnyAsync(r =>
                    r.PropertyId == application.PropertyId &&
                    r.TenantId == tenantId);


            if (alreadyRated)
            {
                TempData["RatingError"] =
                    "You have already rated this property.";

                return RedirectToAction("Dashboard");
            }


            ViewBag.PropertyTitle = application.Property.Title;

            ViewBag.PropertyId = application.PropertyId;

            ViewBag.ApplicationId = application.RentalApplicationId;


            return View();
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
        // ============================================================
        // SUBMIT PROPERTY REVIEW
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(
            int applicationId,
            byte rating,
            string comment)
        {
            var userIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int tenantId))
            {
                return RedirectToAction("Login", "Account");
            }

            // ========================================================
            // FIND THE TENANT'S APPLICATION
            // ========================================================

            var application = await _context.RentalApplications
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r =>
                    r.RentalApplicationId == applicationId &&
                    r.TenantId == tenantId);

            if (application == null)
            {
                return NotFound();
            }

            // ========================================================
            // CHECK THAT THE APPLICATION WAS APPROVED
            // ========================================================

            if (application.RentalApplicationStatus != "Approved")
            {
                return Forbid();
            }

            // ========================================================
            // CHECK THAT THE LEASE HAS STARTED
            // Tenant can only review after LeaseStartDate
            // ========================================================

            if (!application.LeaseStartDate.HasValue ||
                application.LeaseStartDate.Value >=
                DateOnly.FromDateTime(DateTime.Today))
            {
                TempData["ReviewError"] =
                    "You can only rate the property after your lease has started.";

                return RedirectToAction("Dashboard");
            }

            // ========================================================
            // CHECK RATING VALUE
            // ========================================================

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] =
                    "Please select a rating between 1 and 5 stars.";

                return RedirectToAction("Dashboard");
            }

            // ========================================================
            // CHECK COMMENT
            // ========================================================

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] =
                    "Please enter a review comment.";

                return RedirectToAction("Dashboard");
            }

            // ========================================================
            // CHECK IF TENANT ALREADY REVIEWED THIS PROPERTY
            // ========================================================

            var alreadyReviewed = await _context.Reviews
                .AnyAsync(r =>
                    r.PropertyId == application.PropertyId &&
                    r.TenantId == tenantId);

            if (alreadyReviewed)
            {
                TempData["ReviewError"] =
                    "You have already reviewed this property.";

                return RedirectToAction("Dashboard");
            }

            // ========================================================
            // CREATE REVIEW
            // ========================================================

            var review = new Review
            {
                PropertyId = application.PropertyId,
                TenantId = tenantId,
                Rating = rating,
                Comment = comment.Trim(),
                ReviewDate = DateOnly.FromDateTime(DateTime.Today)
            };

            _context.Reviews.Add(review);

            await _context.SaveChangesAsync();

            // ========================================================
            // SUCCESS
            // ========================================================

            TempData["ReviewSuccess"] =
                "Your review has been submitted successfully!";

            return RedirectToAction("Dashboard");
        }

    }
}