namespace ConsanWebApp.Models
{
    public class MapPin
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameKu { get; set; } = "";
        public string AddressAr { get; set; } = "";
        public string AddressEn { get; set; } = "";
        public string AddressKu { get; set; } = "";
        public string Phone { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string MapEmbedUrl { get; set; } = "";
        public bool IsApproved { get; set; } = false;
        public string SubmittedAt { get; set; } = System.DateTime.UtcNow.ToString("o");
    }
}