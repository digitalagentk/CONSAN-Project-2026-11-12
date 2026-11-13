namespace ConsanWebApp.Models
{
    public class AdminKey
    {
        public int Id { get; set; }
        public string KeyHash { get; set; } = "";
        public string CreatedAt { get; set; } = System.DateTime.UtcNow.ToString("o");
        public bool IsActive { get; set; } = true;
    }
}