namespace ConsanWebApp.Models
{
    public class GuideTopic
    {
        public int Id { get; set; }
        public string Icon { get; set; } = "📘";
        public string TitleAr { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string TitleKu { get; set; } = "";
        public string BodyAr { get; set; } = "";
        public string BodyEn { get; set; } = "";
        public string BodyKu { get; set; } = "";
        public int SortOrder { get; set; } = 0;
    }
}