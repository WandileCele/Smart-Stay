using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class UploadDocumentViewModel
    {
        [Required(ErrorMessage = "Please select a document type.")]
        public string DocumentType { get; set; } = null!;

        [Required(ErrorMessage = "Please select a file.")]
        public IFormFile File { get; set; } = null!;
    }
}