namespace Jobba.Models;

public class Activity
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = "";  // "Note", "StatusChange", "FollowUp"
    public string? Details { get; set; }
}