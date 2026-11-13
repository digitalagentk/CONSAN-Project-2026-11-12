namespace ConsanWebApp.Models
{
    public class GuideEquipment
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameKu { get; set; } = "";
        public string SpecAr { get; set; } = "";
        public string SpecEn { get; set; } = "";
        public string SpecKu { get; set; } = "";
        public string UseAr { get; set; } = "";
        public string UseEn { get; set; } = "";
        public string UseKu { get; set; } = "";
        public int SortOrder { get; set; } = 0;
    }
}