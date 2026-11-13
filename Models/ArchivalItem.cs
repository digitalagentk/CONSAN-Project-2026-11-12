namespace ConsanWebApp.Models
{
    public class ArchivalItem
    {
        public int Id { get; set; }
        public string Year { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string CaptionAr { get; set; } = "";
        public string CaptionEn { get; set; } = "";
        public string CaptionKu { get; set; } = "";
        public string CreatedAt { get; set; } = System.DateTime.UtcNow.ToString("o");
    }
}