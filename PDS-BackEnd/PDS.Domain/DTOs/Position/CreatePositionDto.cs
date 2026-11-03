namespace PDS.Domain.DTOs.Position;

public record CreatePositionDto
{
    public Guid OccupationalGroupId { get; set; }
    public required Guid OrganizationalStructureId { get; set; }
    public required string Name { get; set; }
    public string? Competencies { get; set; }
    public int YearsOfExperienceRequired { get; set; }
    public string? NatureOfWork { get; set; }
    public string? TypicalTasks { get; set; }
    public string? RequiredEducation { get; set; }
    public string? Characteristics { get; set; }
    public string? OtherDesirableRequirements { get; set; }
    public int ProbatoryPeriod { get; set; }
    public int Punctuation { get; set; }
    public int Grade { get; set; }
    public int Code { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public bool IsManager { get; set; }
}