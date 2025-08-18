// src/Jobba/Contracts/ActivityDtos.cs
namespace Jobba.Contracts;

public sealed class ActivityDto
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string? Details { get; set; }
}
