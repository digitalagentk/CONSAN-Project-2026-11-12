namespace ConsanWebApp.Models
{
    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class CreateAccountDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Manager";
    }

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; } = "";
    }

    public class AccountListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsVerified { get; set; }
        public string CreatedAt { get; set; } = "";
    }
}
