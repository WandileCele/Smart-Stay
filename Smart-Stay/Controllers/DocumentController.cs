using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Stay.Data;
using Smart_Stay.Models;
using System.Security.Claims;

namespace Smart_Stay.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly SmartDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentController(
            SmartDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Document/Upload
        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            int userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var documents = await _context.Documents
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            ViewBag.Documents = documents;

            return View();
        }


        // POST: /Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            UploadDocumentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Maximum file size = 5 MB
            const long maxFileSize = 5 * 1024 * 1024;

            if (model.File.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "File",
                    "File size must not exceed 5 MB."
                );

                return View(model);
            }

            // Allowed file types
            string[] allowedExtensions =
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            string extension =
                Path.GetExtension(model.File.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "File",
                    "Only PDF, JPG, JPEG and PNG files are allowed."
                );

                return View(model);
            }

            // Get logged-in user's ID
            int userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            // Create wwwroot/uploads/documents
            string uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "documents"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Create unique filename
            string fileName =
                Guid.NewGuid().ToString() + extension;

            string filePath =
                Path.Combine(uploadFolder, fileName);

            // Save physical file
            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            // Save database record
            var document = new Document
            {
                UserId = userId,

                DocumentType = model.DocumentType,

                UploadDate = DateOnly.FromDateTime(
                    DateTime.Now
                ),

                DocumentPath =
                    "/uploads/documents/" + fileName
            };

            _context.Documents.Add(document);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Document uploaded successfully.";

            return RedirectToAction(nameof(Upload));
        }
    }
}