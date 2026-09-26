using System;
using System.ComponentModel.DataAnnotations;

namespace SDP1.Models
{
    public class DisabilityUser
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

        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        [Required]
        public string DisabilityType { get; set; } = string.Empty;

        public string? OtherDisabilityType { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Interests { get; set; }
        public string? ProfilePicture { get; set; }
        public string? DisabilityCertificate { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? LastAction { get; set; }
        public DateTime? LastPasswordChangedAt { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public virtual ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
        public virtual ICollection<HealthcareAppointment> HealthcareAppointments { get; set; } = new List<HealthcareAppointment>();
        public virtual ICollection<HealthcareReview> HealthcareReviews { get; set; } = new List<HealthcareReview>();
        public virtual ICollection<VolunteerSupportRequest> VolunteerSupportRequests { get; set; } = new List<VolunteerSupportRequest>();
        public virtual ICollection<TrainingRegistration> TrainingRegistrations { get; set; } = new List<TrainingRegistration>();
        public virtual ICollection<ScholarshipApplication> ScholarshipApplications { get; set; } = new List<ScholarshipApplication>();
        public virtual ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
        public virtual ICollection<SavedOpportunity> SavedOpportunities { get; set; } = new List<SavedOpportunity>();
    }
}