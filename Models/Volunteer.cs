using System;
using System.ComponentModel.DataAnnotations;

namespace SDP1.Models
{
    public class Volunteer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Skills { get; set; } = string.Empty;

        public string? Availability { get; set; }
        public string? Experience { get; set; }
        public string? LanguagesSpoken { get; set; }
        public string? ProfilePicture { get; set; }

        public bool IsVerified { get; set; } = false;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? District { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        
        [StringLength(300)]
        public string? AssistanceTypes { get; set; } = "Hospital Visits, Shopping Assistance, Travel Assistance, Document Submission, Daily Support";

        [StringLength(500)]
        public string? Bio { get; set; }

        public bool IsAvailable { get; set; } = true;

        [StringLength(30)]
        public string VolunteerStatus { get; set; } = "Active"; 
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? LastAction { get; set; }
        public DateTime? LastPasswordChangedAt { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public virtual ICollection<VolunteerSupportRequest> AssignedRequests { get; set; } = new List<VolunteerSupportRequest>();
    }
}