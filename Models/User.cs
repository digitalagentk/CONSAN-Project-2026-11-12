namespace ConsanWebApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "Manager"; // "Admin" | "Manager"
        public bool IsVerified { get; set; } = false;
        public string Status { get; set; } = "Active";
        public string RegisteredAt { get; set; } = System.DateTime.UtcNow.ToString("o");
        public string LastLoginAt { get; set; } = "";
    }
}