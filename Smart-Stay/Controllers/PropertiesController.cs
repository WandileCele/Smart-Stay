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

        public PropertiesController(
            SmartDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> viewAll(
            string? search,
            string? location,
            string? price)
        {
            var query = _context.Properties
                .Where(p => p.Status == "Available")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.Location.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(p =>
                    p.Location == location);
            }

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
                            _context.Documents.Where(
                                d => d.DocumentType == "Image"),
                            la => la.ListingApplicationId,
                            d => d.ListingApplication,
                            (la, d) => d.DocumentPath
                        )
                        .FirstOrDefault() ?? "",

                    Status = p.Status,

                    ApplicationCount =
                        p.RentalApplications.Count(),

                    AverageRating = p.Reviews.Any()
                        ? p.Reviews.Average(
                            r => (double)r.Rating)
                        : null,

                    ReviewCount = p.Reviews.Count()
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Location = location;
            ViewBag.Price = price;

            return View(properties);
        }

        public async Task<IActionResult> Details(
            int id,
            string? returnUrl)
        {
            var property = await _context.Properties
                .Include(p => p.RentalApplications)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.Tenant)
                .FirstOrDefaultAsync(
                    p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            var imagePaths = await _context.Documents
                .Where(d =>
                    d.DocumentType == "Image" &&
                    d.ListingApplicationNavigation.PropertyId == id)
                .OrderBy(d => d.DocumentId)
                .Select(d => d.DocumentPath)
                .ToListAsync();

            string? candidateReturnUrl =
                returnUrl ??
                Request.Headers["Referer"].ToString();

            string safeReturnUrl =
                (!string.IsNullOrWhiteSpace(candidateReturnUrl) &&
                 Url.IsLocalUrl(candidateReturnUrl))
                    ? candidateReturnUrl
                    : Url.Action("Index", "Home")!;

            bool canApply = true;
            string? applyBlockedReason = null;

            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Tenant"))
            {
                var tenantIdString =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (int.TryParse(
                    tenantIdString,
                    out int tenantId))
                {
                    var existing = property.RentalApplications
                        .Where(a => a.TenantId == tenantId)
                        .OrderByDescending(
                            a => a.ApplicationDate)
                        .FirstOrDefault();

                    if (existing != null &&
                        existing.RentalApplicationStatus != "Rejected")
                    {
                        canApply = false;

                        applyBlockedReason =
                            existing.RentalApplicationStatus ==
                            "Pending"
                                ? "Your application for this property is pending review."
                                : "You already have an approved application for this property.";
                    }
                }
            }

            var model = new PropertyDetailsViewModel
            {
                Property = property,
                ImagePaths = imagePaths,
                ReturnUrl = safeReturnUrl,
                CanApply = canApply,
                ApplyBlockedReason = applyBlockedReason
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(
                    p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            var existingImages = await _context.Documents
                .Where(d =>
                    d.DocumentType == "Image" &&
                    d.ListingApplicationNavigation.PropertyId == id)
                .OrderBy(d => d.DocumentId)
                .Select(d => new ExistingPropertyImageViewModel
                {
                    DocumentId = d.DocumentId,
                    ImagePath = d.DocumentPath
                })
                .ToListAsync();

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
                ExistingImages = existingImages
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            PropertyEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ExistingImages =
                    await _context.Documents
                        .Where(d =>
                            d.DocumentType == "Image" &&
                            d.ListingApplicationNavigation.PropertyId ==
                            model.PropertyId)
                        .OrderBy(d => d.DocumentId)
                        .Select(d =>
                            new ExistingPropertyImageViewModel
                            {
                                DocumentId = d.DocumentId,
                                ImagePath = d.DocumentPath
                            })
                        .ToListAsync();

                return View(model);
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(
                    p => p.PropertyId == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }

            property.Title = model.Title;
            property.Description = model.Description;
            property.Location = model.Location;
            property.Price = model.Price;
            property.PropertyType = model.PropertyType;
            property.Bedrooms = model.Bedrooms;
            property.Bathrooms = model.Bathrooms;

            if (model.ImagesToDelete != null &&
                model.ImagesToDelete.Any())
            {
                var imagesToDelete =
                    await _context.Documents
                        .Where(d =>
                            model.ImagesToDelete.Contains(
                                d.DocumentId) &&
                            d.DocumentType == "Image" &&
                            d.ListingApplicationNavigation.PropertyId ==
                            model.PropertyId)
                        .ToListAsync();

                foreach (var image in imagesToDelete)
                {
                    if (!string.IsNullOrEmpty(
                        image.DocumentPath))
                    {
                        string relativePath =
                            image.DocumentPath
                                .TrimStart('/')
                                .Replace(
                                    "/",
                                    Path.DirectorySeparatorChar
                                        .ToString());

                        string physicalPath =
                            Path.Combine(
                                _environment.WebRootPath,
                                relativePath);

                        if (System.IO.File.Exists(
                            physicalPath))
                        {
                            System.IO.File.Delete(
                                physicalPath);
                        }
                    }
                }

                _context.Documents.RemoveRange(
                    imagesToDelete);
            }

            if (model.NewImages != null &&
                model.NewImages.Any())
            {
                var listingApplication =
                    await _context.ListingApplications
                        .Where(la =>
                            la.PropertyId ==
                            model.PropertyId)
                        .OrderByDescending(
                            la =>
                                la.ListingApplicationId)
                        .FirstOrDefaultAsync();

                if (listingApplication == null)
                {
                    return NotFound();
                }

                int userId = int.Parse(
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier)!);

                string uploadFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(
                        uploadFolder);
                }

                foreach (var image in model.NewImages)
                {
                    if (image == null ||
                        image.Length == 0)
                    {
                        continue;
                    }

                    string imageName =
                        Guid.NewGuid() +
                        Path.GetExtension(
                            image.FileName);

                    string imagePath =
                        Path.Combine(
                            uploadFolder,
                            imageName);

                    using (var stream =
                        new FileStream(
                            imagePath,
                            FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    _context.Documents.Add(
                        new Document
                        {
                            UserId = userId,
                            ListingApplication =
                                listingApplication
                                    .ListingApplicationId,
                            RentalApplicationId = null,
                            DocumentType = "Image",
                            UploadDate =
                                DateOnly.FromDateTime(
                                    DateTime.Now),
                            DocumentPath =
                                "/uploads/" + imageName
                        });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Property updated successfully.";

            return RedirectToAction(
                "Dashboard",
                "Landlord");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(
            int id)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(
                    p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            var model = new PropertyStatusViewModel
            {
                PropertyId = property.PropertyId,
                PropertyTitle = property.Title,
                Status = property.Status
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            PropertyStatusViewModel model)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(
                    p => p.PropertyId == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }

            property.Status = model.Status;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Dashboard",
                "Landlord");
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
                .FirstOrDefaultAsync(
                    p => p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            var listingIds =
                property.ListingApplications
                    .Select(l =>
                        l.ListingApplicationId)
                    .ToList();

            var listingDocuments =
                await _context.Documents
                    .Where(d =>
                        d.ListingApplication != null &&
                        listingIds.Contains(
                            d.ListingApplication.Value))
                    .ToListAsync();

            _context.Documents.RemoveRange(
                listingDocuments);

            var rentalIds =
                property.RentalApplications
                    .Select(r =>
                        r.RentalApplicationId)
                    .ToList();

            var rentalDocuments =
                await _context.Documents
                    .Where(d =>
                        d.RentalApplicationId != null &&
                        rentalIds.Contains(
                            d.RentalApplicationId.Value))
                    .ToListAsync();

            _context.Documents.RemoveRange(
                rentalDocuments);

            _context.Reviews.RemoveRange(
                property.Reviews);

            _context.RentalApplications.RemoveRange(
                property.RentalApplications);

            _context.ListingApplications.RemoveRange(
                property.ListingApplications);

            _context.Properties.Remove(property);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Dashboard",
                "Landlord");
        }

        public IActionResult ListYourProperty()
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!User.IsInRole("Landlord"))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            return RedirectToAction(
                "Create",
                "Properties");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PropertyCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.PropertyImages == null ||
                model.PropertyImages.Count < 3)
            {
                ModelState.AddModelError(
                    "PropertyImages",
                    "Please upload at least 3 property images.");

                return View(model);
            }

            int landlordId = int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

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
                DateListed =
                    DateOnly.FromDateTime(
                        DateTime.Now),
                Status = "Pending"
            };

            _context.Properties.Add(property);

            await _context.SaveChangesAsync();

            int? adminId = await _context.Admins
                .Select(a => (int?)a.UserId)
                .FirstOrDefaultAsync();

            if (adminId == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No admin account is available to review this listing. Please contact support.");

                return View(model);
            }

            var listingApplication =
                new ListingApplication
                {
                    PropertyId =
                        property.PropertyId,

                    LandlordId =
                        landlordId,

                    AdminId =
                        adminId.Value,

                    ApplicationStatus =
                        "Pending",

                    ApplicationDate =
                        DateOnly.FromDateTime(
                            DateTime.Now)
                };

            _context.ListingApplications.Add(
                listingApplication);

            await _context.SaveChangesAsync();

            string uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(
                    uploadFolder);
            }

            if (model.Affidavit != null)
            {
                string affidavitName =
                    Guid.NewGuid() +
                    Path.GetExtension(
                        model.Affidavit.FileName);

                string affidavitPath =
                    Path.Combine(
                        uploadFolder,
                        affidavitName);

                using (var stream =
                    new FileStream(
                        affidavitPath,
                        FileMode.Create))
                {
                    await model.Affidavit.CopyToAsync(
                        stream);
                }

                _context.Documents.Add(
                    new Document
                    {
                        UserId = landlordId,

                        ListingApplication =
                            listingApplication
                                .ListingApplicationId,

                        RentalApplicationId = null,

                        DocumentType = "Affidavit",

                        UploadDate =
                            DateOnly.FromDateTime(
                                DateTime.Now),

                        DocumentPath =
                            "/uploads/" +
                            affidavitName
                    });
            }

            foreach (var image in model.PropertyImages)
            {
                string imageName =
                    Guid.NewGuid() +
                    Path.GetExtension(
                        image.FileName);

                string imagePath =
                    Path.Combine(
                        uploadFolder,
                        imageName);

                using (var stream =
                    new FileStream(
                        imagePath,
                        FileMode.Create))
                {
                    await image.CopyToAsync(
                        stream);
                }

                _context.Documents.Add(
                    new Document
                    {
                        UserId = landlordId,

                        ListingApplication =
                            listingApplication
                                .ListingApplicationId,

                        RentalApplicationId = null,

                        DocumentType = "Image",

                        UploadDate =
                            DateOnly.FromDateTime(
                                DateTime.Now),

                        DocumentPath =
                            "/uploads/" +
                            imageName
                    });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Property submitted successfully. Waiting for Admin approval.";

            return RedirectToAction(
                "Dashboard",
                "Landlord");
        }
    }
}