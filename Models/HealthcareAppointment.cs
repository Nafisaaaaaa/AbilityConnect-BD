using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class HealthcareAppointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HealthcareProviderId { get; set; }

        [ForeignKey("HealthcareProviderId")]
        public virtual HealthcareProvider? HealthcareProvider { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Time slot is required")]
        [StringLength(50)]
        public string TimeSlot { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reason for visit is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Please describe the reason for visit (minimum 5 characters)")]
        public string ReasonForVisit { get; set; } = string.Empty;

        [StringLength(500)]
        public string? PatientNotes { get; set; } 

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; 

        [StringLength(500)]
        public string? DoctorNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
