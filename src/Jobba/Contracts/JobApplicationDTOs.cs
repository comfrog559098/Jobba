namespace Jobba.Contracts;

using Jobba.Models;

public class JobApplicationCreateDto
{
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Source { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? Location { get; set; }
    public string? SalaryRange { get; set; }
    public DateTime? AppliedAt { get; set; } // optional on create
    public string? NextAction { get; set; }
    public string? Notes { get; set; }
}

public sealed class JobApplicationUpdateDto : JobApplicationCreateDto { }

public sealed class JobApplicationListItemDto
{
    public int Id { get; set; }
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
}

public static class JobApplicationMappings
{
    public static JobApplication ToEntity(this JobApplicationCreateDto dto) => new()
    {
        Company = dto.Company.Trim(),
        Role = dto.Role.Trim(),
        Source = dto.Source?.Trim(),
        Status = dto.Status,
        Location = dto.Location?.Trim(),
        SalaryRange = dto.SalaryRange?.Trim(),
        AppliedAt = dto.AppliedAt ?? DateTime.UtcNow,
        NextAction = dto.NextAction,
        Notes = dto.Notes
    };

    public static void Apply(this JobApplication entity, JobApplicationUpdateDto dto)
    {
        entity.Company = dto.Company.Trim();
        entity.Role = dto.Role.Trim();
        entity.Source = dto.Source?.Trim();
        entity.Status = dto.Status;
        entity.Location = dto.Location?.Trim();
        entity.SalaryRange = dto.SalaryRange?.Trim();
        entity.AppliedAt = dto.AppliedAt ?? entity.AppliedAt;
        entity.NextAction = dto.NextAction;
        entity.Notes = dto.Notes;
    }
}
