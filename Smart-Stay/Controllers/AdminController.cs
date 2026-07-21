using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;


namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SmartDbContext _context;

        public AdminController(SmartDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel();
            int adminUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            model.FirstName = await _context.Users
                .Where(u => u.UserId == adminUserId)
                .Select(u => u.FirstName)
                .FirstOrDefaultAsync() ?? "Admin";

            model.PendingApplications = await _context.ListingApplications
                .CountAsync(a => a.ApplicationStatus == "Pending");

            model.ApprovedApplications = await _context.ListingApplications
                .CountAsync(a => a.ApplicationStatus == "Approved");

            model.RejectedApplications = await _context.ListingApplications
                .CountAsync(a => a.ApplicationStatus == "Rejected");

            model.Applications = await _context.ListingApplications

     .Include(a => a.Property)

     .Include(a => a.Landlord)
         .ThenInclude(l => l.User)

     .Where(a => a.ApplicationStatus == "Pending")

     .Select(a => new ListingApplicationCardViewModel
     {
         ListingApplicationId = a.ListingApplicationId,

         PropertyId = a.PropertyId ?? 0,

         PropertyTitle = a.Property != null
             ? a.Property.Title
             : "",

         LandlordName = a.Landlord != null
             ? a.Landlord.User.FirstName + " " + a.Landlord.User.SurName
             : "",

         Location = a.Property != null
             ? a.Property.Location
             : "",

         Price = a.Property != null
             ? a.Property.Price
             : 0,

         PropertyType = a.Property != null
             ? a.Property.PropertyType
             : "",

         ApplicationDate = a.ApplicationDate,

         ApplicationStatus = a.ApplicationStatus
     })

     .ToListAsync();

            return View(model);
        }
    
    [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.ListingApplications
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a => a.ListingApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.ApplicationStatus = "Approved";

            if (application.Property != null)
            {
                application.Property.Status = "Approved";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var application = await _context.ListingApplications
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a => a.ListingApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.ApplicationStatus = "Rejected";

            if (application.Property != null)
            {
                application.Property.Status = "Rejected";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application = await _context.ListingApplications
                .Include(a => a.Property)
                .Include(a => a.Landlord)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(a => a.ListingApplicationId == id);

            if (application == null || application.Property == null)
            {
                return NotFound();
            }

            var documents = await _context.Documents
                .Where(d => d.ListingApplication == id)
                .ToListAsync();

            var model = new ListingApplicationReviewViewModel
            {
                ListingApplicationId = application.ListingApplicationId,
                ApplicationStatus = application.ApplicationStatus,
                ApplicationDate = application.ApplicationDate,

                PropertyId = application.Property.PropertyId,
                Title = application.Property.Title,
                Description = application.Property.Description,
                Location = application.Property.Location,
                Price = application.Property.Price,
                PropertyType = application.Property.PropertyType,
                Bedrooms = application.Property.Bedrooms,
                Bathrooms = application.Property.Bathrooms,

                LandlordName = application.Landlord != null
                    ? application.Landlord.User.FirstName + " " + application.Landlord.User.SurName
                    : "",

                AffidavitPath = documents
                    .Where(d => d.DocumentType == "Affidavit")
                    .Select(d => d.DocumentPath)
                    .FirstOrDefault(),

                ImagePaths = documents
                    .Where(d => d.DocumentType == "Image")
                    .Select(d => d.DocumentPath)
                    .ToList()
            };

            return View(model);
        }
    }
}