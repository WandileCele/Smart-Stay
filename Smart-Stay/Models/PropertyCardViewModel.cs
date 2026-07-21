namespace Smart_Stay.Models
{
    public class PropertyCardViewModel
    {


        public int PropertyID { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public string ImagePath { get; set; }
        public string Status { get; set; } = string.Empty;

        public int ApplicationCount { get; set; }
    }

}


