using System.Collections.Generic;

namespace Smart_Stay.Models
{
    public class PropertyDetailsViewModel
    {
        public Property Property { get; set; } = null!;
        public List<string> ImagePaths { get; set; } = new List<string>();
    }
}