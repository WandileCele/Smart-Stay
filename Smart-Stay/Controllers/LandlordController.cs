using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class LandlordController : Controller
    {
        private readonly SmartDbContext _context;

        public LandlordController(SmartDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            int landlordId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var landlord = await _context.Landlords
          .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.UserId == landlordId);

            var model = new LandlordDashboardViewModel
            {
                FirstName = landlord.User.FirstName
            };

            model.TotalProperties = await _context.Properties
                .CountAsync(p => p.LandlordId == landlordId);

            model.AvailableProperties = await _context.Properties
                .CountAsync(p => p.LandlordId == landlordId &&
                                 p.Status == "Available");
        
            model.TotalApplications = await _context.RentalApplications
                .CountAsync(r => r.LandlordId == landlordId);

            model.Properties = await _context.Properties
          .Where(p => p.LandlordId == landlordId)
          .Select(p => new PropertyCardViewModel
          {
           PropertyID = p.PropertyId,
          Title = p.Title,
          Location = p.Location,
          Price = p.Price,
         Bedrooms = p.Bedrooms ?? 0,
         Bathrooms = p.Bathrooms ?? 0,
         ImagePath = p.ImagePath,
         Status = p.Status,
         ApplicationCount = p.RentalApplications.Count()
     })
     .ToListAsync();

            return View(model);
        }
    }
}