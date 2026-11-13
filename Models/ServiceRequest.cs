namespace ConsanWebApp.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public string Message { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string SubmittedAt { get; set; } = System.DateTime.UtcNow.ToString("o");
    }
}