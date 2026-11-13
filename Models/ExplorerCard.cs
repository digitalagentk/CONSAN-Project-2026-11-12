namespace ConsanWebApp.Models
{
    public class ExplorerCard
    {
        public int Id { get; set; }
        public string Icon { get; set; } = "⚙️";
        public string ImageUrl { get; set; } = "";
        public string TitleAr { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string TitleKu { get; set; } = "";
        public string DescAr { get; set; } = "";
        public string DescEn { get; set; } = "";
        public string DescKu { get; set; } = "";
        public string Tags { get; set; } = "";
        public string Link { get; set; } = "/guide";
        public int SortOrder { get; set; } = 0;
        public string CreatedAt { get; set; } = System.DateTime.UtcNow.ToString("o");
    }
}