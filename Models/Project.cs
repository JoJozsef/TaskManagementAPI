namespace TaskManagementAPI.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } 
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
