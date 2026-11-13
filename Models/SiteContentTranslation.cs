namespace ConsanWebApp.Models
{
    public class SiteContentTranslation
    {
        public int Id { get; set; }
        public string Lang { get; set; } = "ar";
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}