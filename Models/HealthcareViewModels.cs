using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SDP1.Models
{
    public class HealthcareFilterViewModel
    {
        public string? Keyword { get; set; }
        public string? ProviderType { get; set; }
        public string? Specialization { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? SortBy { get; set; } = "newest";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public List<HealthcareProviderCardViewModel> Providers { get; set; } = new List<HealthcareProviderCardViewModel>();
        public List<string> AvailableProviderTypes { get; set; } = new List<string>();
        public List<string> AvailableDistricts { get; set; } = new List<string>();
    }

    public class HealthcareProviderCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public string OrganizationOrClinic { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal? ConsultationFee { get; set; }
        public string AvailabilitySchedule { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public List<string> ServicesList { get; set; } = new List<string>();
    }

    public class HealthcareProviderDetailsViewModel
    {
        public HealthcareProvider Provider { get; set; } = null!;
        public string GoogleMapsApiKey { get; set; } = string.Empty;
        public bool HasGoogleMapsKey => !string.IsNullOrWhiteSpace(GoogleMapsApiKey);
        public HealthcareAppointment? UserLatestAppointment { get; set; }
        public List<string> ServicesList { get; set; } = new List<string>();
    }

    public class BookAppointmentViewModel
    {
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public string OrganizationOrClinic { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string AvailabilitySchedule { get; set; } = string.Empty;
        public decimal? ConsultationFee { get; set; }
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Please select an available time slot")]
        public string TimeSlot { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reason for visit is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Please describe the reason for visit (minimum 5 characters)")]
        public string ReasonForVisit { get; set; } = string.Empty;

        [StringLength(500)]
        public string? PatientNotes { get; set; }

        public List<string> PredefinedSlots { get; set; } = new List<string>
        {
            "09:00 AM - 09:30 AM",
            "09:30 AM - 10:00 AM",
            "10:00 AM - 10:30 AM",
            "10:30 AM - 11:00 AM",
            "11:00 AM - 11:30 AM",
            "03:00 PM - 03:30 PM",
            "03:30 PM - 04:00 PM",
            "04:00 PM - 04:30 PM",
            "04:30 PM - 05:00 PM",
            "05:00 PM - 05:30 PM",
            "06:00 PM - 06:30 PM",
            "06:30 PM - 07:00 PM"
        };
    }

    public class HealthcareProviderFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Provider name is required")]
        [StringLength(150, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provider type is required")]
        public string ProviderType { get; set; } = "Doctor";

        [Required(ErrorMessage = "Organization / Clinic name is required")]
        [StringLength(200)]
        public string OrganizationOrClinic { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(150)]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
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

        [Required(ErrorMessage = "Available services are required")]
        [StringLength(500)]
        public string AvailableServices { get; set; } = string.Empty;

        public decimal? ConsultationFee { get; set; }

        [Required(ErrorMessage = "Availability schedule is required")]
        [StringLength(200)]
        public string AvailabilitySchedule { get; set; } = "Sat - Wed: 4:00 PM - 8:00 PM";

        public int? OrganizationId { get; set; }
    }

    public class UpdateAppointmentStatusModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty; 

        [StringLength(500)]
        public string? DoctorNotes { get; set; }
    }
}
