using PDS.Domain.DTOs.OccupationalGroups;
using PDS.Domain.DTOs.OrganizationalStructure;

namespace PDS.Domain.DTOs.Position;

public record PositionDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Competencies { get; init; }
    public int YearsOfExperienceRequired { get; init; }
    public string? NatureOfWork { get; set; }
    public string? TypicalTasks { get; set; }
    public string? RequiredEducation { get; set; }
    public string? Characteristics { get; set; }
    public string? OtherDesirableRequirements { get; set; }
    public int ProbatoryPeriod { get; set; }
    public int Punctuation { get; set; }
    public int Grade { get; set; }
    public int Code { get; set; }
    public decimal MinSalary { get; init; }
    public decimal MaxSalary { get; init; }
    public bool IsManager { get; init; }
    public required OccupationalGroupDto OccupationalGroup { get; init; }
    public required OrganizationalStructureWhioutDependenciesDto OrganizationalStructure { get; init; }
}