namespace Jobba.Models;

public enum ApplicationStatus
{
    Draft = 0,
    Applied = 1,
    Screening = 2,
    Interview = 3,
    Offer = 4,
    Rejected = 5,
    Withdrawn = 6
}

public class JobApplication
{
    public int Id { get; set; }
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Source { get; set; }         // url of job application (or job board, linkedin post, indeed post, etc.)
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? Location { get; set; }
    public string? SalaryRange { get; set; }     // “80–100k CAD”
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string? NextAction { get; set; }      // “Email recruiter Friday”
    public string? Notes { get; set; }
    public List<Activity> Activities { get; set; } = [];
}
