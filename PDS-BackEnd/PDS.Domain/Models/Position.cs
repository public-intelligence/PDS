using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PDS.Domain.Models;

public class Position : ISoftDeletable
{
    public Guid Id { get; set; }

    public Guid OrganizationalStructureId { get; set; }
    public Guid OccupationalGroupId { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Competencies { get; set; }

    [Range(0, int.MaxValue)]
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

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navegación
    public OrganizationalStructure? OrganizationalStructure { get; set; }
    public OccupationalGroup? OccupationalGroup { get; set; }
    //public ICollection<PersonnelPosition> PersonnelPositions { get; set; } = [];


}