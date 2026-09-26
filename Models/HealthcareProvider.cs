using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class HealthcareProvider
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Provider name is required")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 150 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provider type is required")]
        [StringLength(60)]
        public string ProviderType { get; set; } = "Doctor"; 

        [Required(ErrorMessage = "Organization or clinic name is required")]
        [StringLength(200)]
        public string OrganizationOrClinic { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(150)]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "District is required")]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Available services are required")]
        [StringLength(500)]
        public string AvailableServices { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ConsultationFee { get; set; }

        [Required(ErrorMessage = "Availability schedule is required")]
        [StringLength(200)]
        public string AvailabilitySchedule { get; set; } = "Sat - Wed: 4:00 PM - 8:00 PM";

        [StringLength(500)]
        public string? ProfileImage { get; set; }

        public int? OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<HealthcareAppointment> Appointments { get; set; } = new List<HealthcareAppointment>();
        public virtual ICollection<HealthcareReview> Reviews { get; set; } = new List<HealthcareReview>();
    }
}
