namespace AncientServer.Models.DTOs
{
    public class UpdateProfileRequest
    {
        public string? AvatarUrl { get; set; }
        public string? Description { get; set; }
        public string? Language { get; set; }
        public bool? NotificationsEnabled { get; set; }
    }
}