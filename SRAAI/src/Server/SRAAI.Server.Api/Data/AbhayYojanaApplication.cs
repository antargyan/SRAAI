using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SRAAI.Server.Api.Data;

[Table("AbhayYojanaApplications")]
public class AbhayYojanaApplication
{
    

    [Key]
    [Column("OriginalSlumNumber")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int OriginalSlumNumber { get; set; } // Primary Key (2nd column)


    [Column("SerialNumber")]
    public int? SerialNumber { get; set; }

    [Column("OriginalSlumDwellerName")]
    [MaxLength(200)]
    public string OriginalSlumDwellerName { get; set; } = string.Empty;

    [Column("ApplicantName")]
    [MaxLength(200)]
    public string ApplicantName { get; set; } = string.Empty;

    [Column("VoterListYear")]
    public int? VoterListYear { get; set; }

    [Column("VoterListPartNumber")]
    [MaxLength(50)]
    public string? VoterListPartNumber { get; set; }

    [Column("VoterListSerialNumber")]
    public int? VoterListSerialNumber { get; set; }

    [Column("VoterListBound")]
    [MaxLength(100)]
    public string? VoterListBound { get; set; }

    [Column("SlumUsage")]
    [MaxLength(100)]
    public string SlumUsage { get; set; } = string.Empty; // e.g., "निवासी" (Residential)

    [Column("CarpetAreaSqFt", TypeName = "decimal(18,2)")]
    public decimal? CarpetAreaSqFt { get; set; }

    [Column("EvidenceDetails")]
    [MaxLength(2000)]
    public string EvidenceDetails { get; set; } = string.Empty;

    [Column("EligibilityStatus")]
    [MaxLength(50)]
    public string EligibilityStatus { get; set; } = string.Empty; // पात्र/अपात्र/अनिर्णित

    [Column("Remarks")]
    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("UpdatedDate")]
    public DateTime? UpdatedDate { get; set; }

    [Column("Version")]
    public int Version { get; set; } = 1; // Version number for tracking Excel imports
    [NotMapped]
    public string Status { get; set; }
    [NotMapped]
    public List<string> ChangedFields { get; set; } = new();
}
