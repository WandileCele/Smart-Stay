using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    public class PropertiesController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PropertiesController(SmartDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

       
        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties
                .Include(p => p.RentalApplications)
                .FirstOrDefaultAsync(p => p.PropertyId == id);


            if (property == null)
            {
                return NotFound();
            }


            return View(property);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }



        [Authorize]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var property = _context.Properties
                .FirstOrDefault(p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            var model = new PropertyEditViewModel
            {
                PropertyId = property.PropertyId,
                Title = property.Title,
                Description = property.Description,
                Location = property.Location,
                Price = property.Price,
                PropertyType = property.PropertyType,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                ExistingImagePath = property.ImagePath
            };

            return View(model);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PropertyEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }


            // Update property details

            property.Title = model.Title;
            property.Description = model.Description;
            property.Location = model.Location;
            property.Price = model.Price;
            property.PropertyType = model.PropertyType;
            property.Bedrooms = model.Bedrooms;
            property.Bathrooms = model.Bathrooms;


            // If a new image was uploaded
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "properties"
                );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }


                string fileName = Guid.NewGuid().ToString()
                                  + Path.GetExtension(model.ImageFile.FileName);


                string filePath = Path.Combine(
                    uploadsFolder,
                    fileName
                );


                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }


                property.ImagePath = "/images/properties/" + fileName;
            }


            await _context.SaveChangesAsync();


            return RedirectToAction("Dashboard", "Landlord");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }


            var model = new PropertyStatusViewModel
            {
                PropertyId = property.PropertyId,
                Status = property.Status
            };


            return View(model);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(PropertyStatusViewModel model)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == model.PropertyId);


            if (property == null)
            {
                return NotFound();
            }


            property.Status = model.Status;


            await _context.SaveChangesAsync();


            return RedirectToAction("Dashboard", "Landlord");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
         
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties
                .Include(p => p.ListingApplications)
                .Include(p => p.RentalApplications)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }


            // Delete documents linked to rental applications
            var rentalApplicationIds = property.RentalApplications
                .Select(r => r.RentalApplicationId)
                .ToList();


            var documents = await _context.Documents
           .Where(d => rentalApplicationIds.Contains(d.RentalApplicationId.Value))
           .ToListAsync();

            if (documents.Any())
            {
                _context.Documents.RemoveRange(documents);
            }


            // Delete reviews
            if (property.Reviews.Any())
            {
                _context.Reviews.RemoveRange(property.Reviews);
            }


            // Delete rental applications
            if (property.RentalApplications.Any())
            {
                _context.RentalApplications.RemoveRange(property.RentalApplications);
            }


            // Delete listing applications
            if (property.ListingApplications.Any())
            {
                _context.ListingApplications.RemoveRange(property.ListingApplications);
            }


            // Delete property
            _context.Properties.Remove(property);


            await _context.SaveChangesAsync();


            return RedirectToAction("Dashboard", "Landlord");
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.PropertyImages == null || model.PropertyImages.Count < 3)
            {
                ModelState.AddModelError("PropertyImages", "Please upload at least 3 property images.");
                return View(model);
            }

            int landlordId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var property = new Property
            {
                LandlordId = landlordId,
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                Price = model.Price,
                PropertyType = model.PropertyType,
                Bedrooms = model.Bedrooms,
                Bathrooms = model.Bathrooms,
                DateListed = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending"
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var listingApplication = new ListingApplication
            {
                PropertyId = property.PropertyId,
                LandlordId = landlordId,
                AdminId = 3, // Replace later with your actual admin selection logic
                ApplicationStatus = "Pending",
                ApplicationDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.ListingApplications.Add(listingApplication);
            await _context.SaveChangesAsync();

            // Folder for uploads
            string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            
            if (model.Affidavit != null)
            {
                string affidavitName = Guid.NewGuid() + Path.GetExtension(model.Affidavit.FileName);

                string affidavitPath = Path.Combine(uploadFolder, affidavitName);

                using (var stream = new FileStream(affidavitPath, FileMode.Create))
                {
                    await model.Affidavit.CopyToAsync(stream);
                }

                _context.Documents.Add(new Document
                {
                    ListingApplication = listingApplication.ListingApplicationId,
                    RentalApplicationId = null,
                    DocumentType = "Affidavit",
                    UploadDate = DateOnly.FromDateTime(DateTime.Now),
                    DocumentPath = "/uploads/" + affidavitName
                });
            }

            // Save Property Images
            foreach (var image in model.PropertyImages)
            {
                string imageName = Guid.NewGuid() + Path.GetExtension(image.FileName);

                string imagePath = Path.Combine(uploadFolder, imageName);

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                _context.Documents.Add(new Document
                {
                    ListingApplication = listingApplication.ListingApplicationId,
                    RentalApplicationId = null,
                    DocumentType = "Image",
                    UploadDate = DateOnly.FromDateTime(DateTime.Now),
                    DocumentPath = "/uploads/" + imageName
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Property submitted successfully. Waiting for Admin approval.";

            return RedirectToAction("Dashboard", "Landlord");
        }
    }
}