using System.ComponentModel.DataAnnotations;

namespace SRAAI.Shared.Dtos.AbhayYojana;

public class AbhayYojanaApplicationDto
{
    public int? SerialNumber { get; set; }
    
    [Required]
    public int OriginalSlumNumber { get; set; } // Primary Key (2nd column)
    
    [Required]
    [MaxLength(200)]
    public string OriginalSlumDwellerName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ApplicantName { get; set; } = string.Empty;
    
    public int? VoterListYear { get; set; }
    
    [MaxLength(50)]
    public string? VoterListPartNumber { get; set; }
    
    public int? VoterListSerialNumber { get; set; }
    
    [MaxLength(100)]
    public string? VoterListBound { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string SlumUsage { get; set; } = string.Empty;
    
    public decimal? CarpetAreaSqFt { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string EvidenceDetails { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EligibilityStatus { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Remarks { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    
    public int Version { get; set; } = 1;
    [NotMapped]
    public string Status { get; set; }
    [NotMapped]
    public List<string> ChangedFields { get; set; } = new();
}

public class CreateAbhayYojanaApplicationDto
{
    public int? SerialNumber { get; set; }
    
    [Required]
    public int OriginalSlumNumber { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string OriginalSlumDwellerName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ApplicantName { get; set; } = string.Empty;
    
    public int? VoterListYear { get; set; }
    
    [MaxLength(50)]
    public string? VoterListPartNumber { get; set; }
    
    public int? VoterListSerialNumber { get; set; }
    
    [MaxLength(100)]
    public string? VoterListBound { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string SlumUsage { get; set; } = string.Empty;
    
    public decimal? CarpetAreaSqFt { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string EvidenceDetails { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EligibilityStatus { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Remarks { get; set; }
    
    public int Version { get; set; } = 1;
}

public class UpdateAbhayYojanaApplicationDto
{
    [Required]
    public int OriginalSlumNumber { get; set; }
    
    [MaxLength(200)]
    public string? OriginalSlumDwellerName { get; set; }
    
    [MaxLength(200)]
    public string? ApplicantName { get; set; }
    
    public int? VoterListYear { get; set; }
    
    [MaxLength(50)]
    public string? VoterListPartNumber { get; set; }
    
    public int? VoterListSerialNumber { get; set; }
    
    [MaxLength(100)]
    public string? VoterListBound { get; set; }
    
    [MaxLength(100)]
    public string? SlumUsage { get; set; }
    
    public decimal? CarpetAreaSqFt { get; set; }
    
    [MaxLength(2000)]
    public string? EvidenceDetails { get; set; }
    
    [MaxLength(50)]
    public string? EligibilityStatus { get; set; }
    
    [MaxLength(1000)]
    public string? Remarks { get; set; }
}
