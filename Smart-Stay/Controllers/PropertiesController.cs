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

        // ============================================================
        // PRICE PERIOD HELPERS (period is stored as a tag in Description)
        // ============================================================

        private static readonly string[] AllowedPricePeriods =
            { "per night", "per month" };

        private static string GetPricePeriod(string? description)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                description ?? "", @"\[PricePeriod:(.+?)\]");

            return match.Success &&
                   AllowedPricePeriods.Contains(match.Groups[1].Value)
                ? match.Groups[1].Value
                : "per month";
        }

        private static string StripPricePeriod(string? description)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                description ?? "", @"\s*\[PricePeriod:.+?\]", "");
        }

        private static string AddPricePeriod(string? description, string pricePeriod)
        {
            if (!AllowedPricePeriods.Contains(pricePeriod))
            {
                pricePeriod = "per month";
            }

            return StripPricePeriod(description) + $" [PricePeriod:{pricePeriod}]";
        }

        [HttpGet]
        public async Task<IActionResult> viewAll(
    string? search,
    string? location,
    string? price)
        {
            // FIXED: Show both Available AND Approved
            var query = _context.Properties
               .Where(p => p.Status == "Available" || p.Status == "Approved")
               .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p => p.Title.Contains(search) || p.Location.Contains(search));
            }

            // ========================================================
            // LOCATION FILTER
            // ========================================================

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(p => p.Location == location);
            }

            // ========================================================
            // PRICE FILTER
            // ========================================================

            if (!string.IsNullOrWhiteSpace(price))
            {
                switch (price)
                {
                    case "0-2000":
                        query = query.Where(p =>
                            p.Price >= 0 && p.Price <= 2000);
                        break;
                    case "2000-3000":
                        query = query.Where(p =>
                            p.Price > 2000 && p.Price <= 3000);
                        break;
                    case "3000-5000":
                        query = query.Where(p =>
                            p.Price > 3000 && p.Price <= 5000);
                        break;
                    case "5000+":
                        query = query.Where(p =>
                            p.Price > 5000);
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

                    PricePeriod =
                        p.Description != null && p.Description.Contains("PricePeriod:per night")
                            ? "per night"
                            : "per month",

                    Bedrooms = p.Bedrooms ?? 0,

                    Bathrooms = p.Bathrooms ?? 0,

                    ImagePath = _context.ListingApplications
                        .Where(la => la.PropertyId == p.PropertyId)
                        .Join(
                            _context.Documents
                                .Where(d => d.DocumentType == "Image"),
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

        public async Task<IActionResult> Details(
            int id,
            string? returnUrl)
        {
            var property = await _context.Properties
                .Include(p => p.RentalApplications)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.Tenant)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

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
                returnUrl ?? Request.Headers["Referer"].ToString();

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
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(tenantIdString, out int tenantId))
                {
                    var existing = property.RentalApplications
                        .Where(a => a.TenantId == tenantId)
                        .OrderByDescending(a => a.ApplicationDate)
                        .FirstOrDefault();

                    if (existing != null &&
                        existing.RentalApplicationStatus != "Rejected")
                    {
                        canApply = false;

                        applyBlockedReason =
                            existing.RentalApplicationStatus == "Pending"
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

        // ============================================================
        // CREATE
        // ============================================================

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ============================================================
        // EDIT GET
        // ============================================================
        // ============================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int landlordId =
                int.Parse(User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id &&
                    p.LandlordId == landlordId);

            if (property == null)
            {
                return NotFound();
            }

            var latestApplication =
                await _context.ListingApplications
                    .Where(a =>
                        a.PropertyId == id &&
                        a.LandlordId == landlordId)
                    .OrderByDescending(a =>
                        a.ListingApplicationId)
                    .FirstOrDefaultAsync();

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
                Description = StripPricePeriod(property.Description),
                Location = property.Location,
                Price = property.Price,
                PropertyType = property.PropertyType,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                ExistingImages = existingImages,

                IsRejected =
                    latestApplication != null &&
                    latestApplication.ApplicationStatus == "Rejected",

                RejectionReason =
                    latestApplication?.RejectionReason
            };

            return View(model);
        }


        // ============================================================
        // EDIT POST
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            PropertyEditViewModel model,
            string? pricePeriod)
        {
            if (!ModelState.IsValid)
            {
                // Reload existing images so they are still displayed
                // if validation fails.
                model.ExistingImages = await _context.Documents
                    .Where(d =>
                        d.DocumentType == "Image" &&
                        d.ListingApplicationNavigation.PropertyId ==
                        model.PropertyId)
                    .OrderBy(d => d.DocumentId)
                    .Select(d => new ExistingPropertyImageViewModel
                    {
                        DocumentId = d.DocumentId,
                        ImagePath = d.DocumentPath
                    })
                    .ToListAsync();

                return View(model);
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == model.PropertyId);

            if (property == null)
            {
                return NotFound();
            }

            // Get the currently logged-in landlord's User ID.
            int landlordId =
                int.Parse(User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // ============================================================
            // UPDATE PROPERTY INFORMATION
            // ============================================================

            property.Title = model.Title;

            var period = (pricePeriod != null &&
                          AllowedPricePeriods.Contains(pricePeriod))
                ? pricePeriod
                : GetPricePeriod(property.Description);

            property.Description = AddPricePeriod(model.Description, period);
            property.Location = model.Location;
            property.Price = model.Price;
            property.PropertyType = model.PropertyType;
            property.Bedrooms = model.Bedrooms;
            property.Bathrooms = model.Bathrooms;

            // ============================================================
            // DELETE SELECTED EXISTING IMAGES
            // ============================================================

            if (model.ImagesToDelete != null &&
                model.ImagesToDelete.Any())
            {
                var imagesToDelete = await _context.Documents
                    .Where(d =>
                        model.ImagesToDelete.Contains(d.DocumentId) &&
                        d.DocumentType == "Image" &&
                        d.ListingApplicationNavigation.PropertyId ==
                        model.PropertyId)
                    .ToListAsync();

                foreach (var image in imagesToDelete)
                {
                    // Delete physical image from wwwroot/uploads
                    if (!string.IsNullOrEmpty(image.DocumentPath))
                    {
                        string relativePath =
                            image.DocumentPath
                                .TrimStart('/')
                                .Replace(
                                    "/",
                                    Path.DirectorySeparatorChar
                                        .ToString());

                        string physicalPath = Path.Combine(
                            _environment.WebRootPath,
                            relativePath);

                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                }

                _context.Documents.RemoveRange(imagesToDelete);
            }

            // ============================================================
            // ADD NEW IMAGES
            // ============================================================

            if (model.NewImages != null &&
                model.NewImages.Any())
            {
                var listingApplication =
                    await _context.ListingApplications
                        .Where(la =>
                            la.PropertyId == model.PropertyId)
                        .OrderByDescending(
                            la => la.ListingApplicationId)
                        .FirstOrDefaultAsync();

                if (listingApplication == null)
                {
                    return NotFound();
                }

                string uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (var image in model.NewImages)
                {
                    if (image == null || image.Length == 0)
                    {
                        continue;
                    }

                    string imageName =
                        Guid.NewGuid() +
                        Path.GetExtension(image.FileName);

                    string imagePath = Path.Combine(
                        uploadFolder,
                        imageName);

                    using (var stream = new FileStream(
                        imagePath,
                        FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    _context.Documents.Add(new Document
                    {
                        UserId = landlordId,
                        ListingApplication =
                            listingApplication.ListingApplicationId,
                        RentalApplicationId = null,
                        DocumentType = "Image",
                        UploadDate =
                            DateOnly.FromDateTime(DateTime.Now),
                        DocumentPath =
                            "/uploads/" + imageName
                    });
                }
            }

            // ============================================================
            // SAVE EVERYTHING
            // ============================================================

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Property updated successfully.";

            return RedirectToAction(
                "Dashboard",
                "Landlord");
        }
        // ============================================================
        // RESUBMIT REJECTED PROPERTY
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubmit(
            PropertyEditViewModel model,
            string? pricePeriod)
        {
            // ============================================================
            // VALIDATION
            // ============================================================

            if (!ModelState.IsValid)
            {
                model.ExistingImages = await _context.Documents
                    .Where(d =>
                        d.DocumentType == "Image" &&
                        d.ListingApplicationNavigation.PropertyId ==
                        model.PropertyId)
                    .OrderBy(d => d.DocumentId)
                    .Select(d => new ExistingPropertyImageViewModel
                    {
                        DocumentId = d.DocumentId,
                        ImagePath = d.DocumentPath
                    })
                    .ToListAsync();

                model.IsRejected = true;

                return View("Edit", model);
            }

            // ============================================================
            // GET LOGGED-IN LANDLORD
            // ============================================================

            int landlordId =
                int.Parse(User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // ============================================================
            // FIND PROPERTY
            // ============================================================

            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == model.PropertyId &&
                    p.LandlordId == landlordId);

            if (property == null)
            {
                return NotFound();
            }

            // ============================================================
            // FIND LATEST LISTING APPLICATION
            // ============================================================

            var previousApplication =
                await _context.ListingApplications
                    .Where(a =>
                        a.PropertyId == model.PropertyId &&
                        a.LandlordId == landlordId)
                    .OrderByDescending(a =>
                        a.ListingApplicationId)
                    .FirstOrDefaultAsync();

            // Property must have been rejected before it can be resubmitted
            if (previousApplication == null ||
                previousApplication.ApplicationStatus != "Rejected")
            {
                return BadRequest(
                    "This property cannot be resubmitted.");
            }

            // ============================================================
            // UPDATE PROPERTY INFORMATION
            // ============================================================

            property.Title = model.Title;

            var period = (pricePeriod != null &&
                          AllowedPricePeriods.Contains(pricePeriod))
                ? pricePeriod
                : GetPricePeriod(property.Description);

            property.Description = AddPricePeriod(model.Description, period);
            property.Location = model.Location;
            property.Price = model.Price;
            property.PropertyType = model.PropertyType;
            property.Bedrooms = model.Bedrooms;
            property.Bathrooms = model.Bathrooms;

            // Property is waiting for Admin review again
            property.Status = "Pending";

            // ============================================================
            // FIND ADMIN
            // ============================================================

            int? adminId = await _context.Admins
                .Select(a => (int?)a.UserId)
                .FirstOrDefaultAsync();

            if (adminId == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No admin account is available to review this listing.");

                model.IsRejected = true;

                model.ExistingImages =
                    await _context.Documents
                        .Where(d =>
                            d.DocumentType == "Image" &&
                            d.ListingApplicationNavigation.PropertyId ==
                            model.PropertyId)
                        .OrderBy(d => d.DocumentId)
                        .Select(d => new ExistingPropertyImageViewModel
                        {
                            DocumentId = d.DocumentId,
                            ImagePath = d.DocumentPath
                        })
                        .ToListAsync();

                return View("Edit", model);
            }

            // ============================================================
            // CREATE NEW LISTING APPLICATION
            // ============================================================

            var newApplication = new ListingApplication
            {
                PropertyId = property.PropertyId,
                LandlordId = landlordId,
                AdminId = adminId.Value,
                ApplicationStatus = "Pending",
                ApplicationDate =
                    DateOnly.FromDateTime(DateTime.Now)
            };

            _context.ListingApplications.Add(newApplication);

            // Save first so we get the new ListingApplicationId
            await _context.SaveChangesAsync();

            // ============================================================
            // REUSE OLD DOCUMENTS
            // ============================================================
            // We create NEW Document records but use the SAME file paths.
            //
            // This means:
            //
            // OLD APPLICATION
            //      ↓
            // Rejected application keeps its documents
            //
            // NEW APPLICATION
            //      ↓
            // Gets new document records pointing to the same files
            //
            // The actual files do NOT need to be uploaded again.
            // ============================================================

            var previousDocuments =
                await _context.Documents
                    .Where(d =>
                        d.ListingApplication ==
                        previousApplication.ListingApplicationId)
                    .ToListAsync();

            foreach (var oldDocument in previousDocuments)
            {
                _context.Documents.Add(new Document
                {
                    UserId = landlordId,

                    ListingApplication =
                        newApplication.ListingApplicationId,

                    RentalApplicationId = null,

                    DocumentType =
                        oldDocument.DocumentType,

                    UploadDate =
                        DateOnly.FromDateTime(DateTime.Now),

                    DocumentPath =
                        oldDocument.DocumentPath
                });
            }

            // ============================================================
            // UPLOAD FOLDER
            // ============================================================

            string uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // ============================================================
            // ADD ANY NEW IMAGES
            // ============================================================
            // The landlord does NOT need to upload the old pictures.
            //
            // However, if they choose to add new pictures, those are
            // uploaded and attached to the new application as well.
            // ============================================================

            if (model.NewImages != null &&
                model.NewImages.Any())
            {
                foreach (var image in model.NewImages)
                {
                    if (image == null || image.Length == 0)
                    {
                        continue;
                    }

                    string imageName =
                        Guid.NewGuid() +
                        Path.GetExtension(image.FileName);

                    string imagePath =
                        Path.Combine(
                            uploadFolder,
                            imageName);

                    using (var stream = new FileStream(
                        imagePath,
                        FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    _context.Documents.Add(new Document
                    {
                        UserId = landlordId,

                        ListingApplication =
                            newApplication.ListingApplicationId,

                        RentalApplicationId = null,

                        DocumentType = "Image",

                        UploadDate =
                            DateOnly.FromDateTime(DateTime.Now),

                        DocumentPath =
                            "/uploads/" + imageName
                    });
                }
            }

            // ============================================================
            // SAVE DOCUMENTS
            // ============================================================

            await _context.SaveChangesAsync();

            // ============================================================
            // SUCCESS
            // ============================================================

            TempData["SuccessMessage"] =
                "Your property has been resubmitted successfully and is waiting for Admin approval.";

            return RedirectToAction(
                "Dashboard",
                "Landlord");
        }

        // ============================================================
        // UPDATE STATUS GET
        // ============================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id);

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

        // ============================================================
        // UPDATE STATUS POST
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            PropertyStatusViewModel model)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == model.PropertyId);

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

        // ============================================================
        // DELETE
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties
                .Include(p => p.ListingApplications)
                .Include(p => p.RentalApplications)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            var listingIds = property.ListingApplications
                .Select(l => l.ListingApplicationId)
                .ToList();

            var listingDocuments = await _context.Documents
                .Where(d =>
                    d.ListingApplication != null &&
                    listingIds.Contains(
                        d.ListingApplication.Value))
                .ToListAsync();

            _context.Documents.RemoveRange(listingDocuments);

            var rentalIds = property.RentalApplications
                .Select(r => r.RentalApplicationId)
                .ToList();

            var rentalDocuments = await _context.Documents
                .Where(d =>
                    d.RentalApplicationId != null &&
                    rentalIds.Contains(
                        d.RentalApplicationId.Value))
                .ToListAsync();

            _context.Documents.RemoveRange(rentalDocuments);

            _context.Reviews.RemoveRange(property.Reviews);

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

        // ============================================================
        // LIST YOUR PROPERTY
        // ============================================================

        public IActionResult ListYourProperty()
        {
            // User is not logged in
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // User is logged in but is NOT a landlord
            if (!User.IsInRole("Landlord"))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // User is a landlord
            return RedirectToAction(
                "Create",
                "Properties");
        }



        // ============================================================
        // CREATE POST
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PropertyCreateViewModel model,
            string? pricePeriod)
        {

            if (!(model.Price > 0))
                ModelState.AddModelError("Price", "Price cannot be zero.");

            if (!(model.Bedrooms >= 1))
                ModelState.AddModelError("Bedrooms", "Bedrooms cannot be zero. Enter at least 1.");

            if (!(model.Bathrooms >= 1))
                ModelState.AddModelError("Bathrooms", "Bathrooms cannot be zero. Enter at least 1.");

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

            int landlordId =
                int.Parse(User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

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


            var property = new Property
            {
                LandlordId = landlordId,
                Title = model.Title,
                Description = AddPricePeriod(model.Description, pricePeriod ?? "per month"),
                Location = model.Location,
                Address = model.Address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Price = model.Price,
                PropertyType = model.PropertyType,
                Bedrooms = model.Bedrooms,
                Bathrooms = model.Bathrooms,
                DateListed =
                    DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending"
            };

            _context.Properties.Add(property);

            await _context.SaveChangesAsync();



            var listingApplication = new ListingApplication
            {
                PropertyId = property.PropertyId,
                LandlordId = landlordId,
                AdminId = adminId.Value,
                ApplicationStatus = "Pending",
                ApplicationDate =
                    DateOnly.FromDateTime(DateTime.Now)
            };

            _context.ListingApplications.Add(
                listingApplication);

            await _context.SaveChangesAsync();


            string uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }


            if (model.Affidavit != null)
            {
                string affidavitName =
                    Guid.NewGuid() +
                    Path.GetExtension(
                        model.Affidavit.FileName);

                string affidavitPath = Path.Combine(
                    uploadFolder,
                    affidavitName);

                using (var stream = new FileStream(
                    affidavitPath,
                    FileMode.Create))
                {
                    await model.Affidavit.CopyToAsync(stream);
                }

                _context.Documents.Add(new Document
                {
                    UserId = landlordId,
                    ListingApplication =
                        listingApplication.ListingApplicationId,
                    RentalApplicationId = null,
                    DocumentType = "Affidavit",
                    UploadDate =
                        DateOnly.FromDateTime(DateTime.Now),
                    DocumentPath =
                        "/uploads/" + affidavitName
                });
            }



            foreach (var image in model.PropertyImages)
            {
                string imageName =
                    Guid.NewGuid() +
                    Path.GetExtension(image.FileName);

                string imagePath = Path.Combine(
                    uploadFolder,
                    imageName);

                using (var stream = new FileStream(
                    imagePath,
                    FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                _context.Documents.Add(new Document
                {
                    UserId = landlordId,
                    ListingApplication =
                        listingApplication.ListingApplicationId,
                    RentalApplicationId = null,
                    DocumentType = "Image",
                    UploadDate =
                        DateOnly.FromDateTime(DateTime.Now),
                    DocumentPath =
                        "/uploads/" + imageName
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