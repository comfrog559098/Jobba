namespace Jobba.Validation;

using Jobba.Contracts;

public static class Validators
{
    public static (bool ok, string? message) Validate(this JobApplicationCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Company)) return (false, "Company is required.");
        if (dto.Company.Length > 200) return (false, "Company max length is 200.");
        if (string.IsNullOrWhiteSpace(dto.Role)) return (false, "Role is required.");
        if (dto.Role.Length > 200) return (false, "Role max length is 200.");
        if (dto.SalaryRange != null && dto.SalaryRange.Length > 100) return (false, "SalaryRange max length is 100.");
        return (true, null);
    }

    public static (bool ok, string? message) Validate(this JobApplicationUpdateDto dto)
        => ((JobApplicationCreateDto)dto).Validate();
}
