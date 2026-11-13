namespace ConsanWebApp.Models
{
    public class HeroImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = "";
        public int SortOrder { get; set; } = 0;
    }
}