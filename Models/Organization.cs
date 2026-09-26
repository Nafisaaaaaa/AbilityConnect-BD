using System;
using System.ComponentModel.DataAnnotations;

namespace SDP1.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        public string OrganizationType { get; set; } = string.Empty;

        public string? OtherOrganizationType { get; set; }

        [Required]
        [StringLength(500)]
        public string OrganizationDescription { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Division { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? Website { get; set; }

        public string? FacebookProfileLink { get; set; }

        [Required]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required]
        public string ContactPersonDesignation { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string ContactPersonPhone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ContactPersonEmail { get; set; } = string.Empty;

        public string? Logo { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? LastAction { get; set; }
        public DateTime? LastPasswordChangedAt { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
        public virtual ICollection<HealthcareProvider> HealthcareProviders { get; set; } = new List<HealthcareProvider>();
        public virtual ICollection<TrainingProgram> TrainingPrograms { get; set; } = new List<TrainingProgram>();
        public virtual ICollection<Scholarship> Scholarships { get; set; } = new List<Scholarship>();
        public virtual ICollection<AwarenessEvent> AwarenessEvents { get; set; } = new List<AwarenessEvent>();
    }
}