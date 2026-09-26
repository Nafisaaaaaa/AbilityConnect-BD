using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        [Required]
        [StringLength(100)]
        public string ApplicantFullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ApplicantEmail { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string ApplicantPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Resume is required")]
        [StringLength(255)]
        public string ResumePath { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ResumeFileName { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? CoverLetter { get; set; }

        [StringLength(1000)]
        public string? AccommodationRequested { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; 

        [StringLength(1000)]
        public string? OrganizationNotes { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }
    }
}
