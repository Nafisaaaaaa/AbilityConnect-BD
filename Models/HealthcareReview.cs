using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class HealthcareReview
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

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review text is required")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Review must be between 5 and 1000 characters")]
        public string ReviewText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
