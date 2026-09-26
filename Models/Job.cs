using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        [Required(ErrorMessage = "Job title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Job title must be between 3 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job type is required")]
        [StringLength(50)]
        public string JobType { get; set; } = "Full-time"; 

        [Required(ErrorMessage = "Workplace / Remote option is required")]
        [StringLength(50)]
        public string WorkplaceType { get; set; } = "Onsite"; 

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Salary { get; set; } 

        [StringLength(500)]
        public string? AccessibilityFeatures { get; set; } 

        public bool FlexibleWorkingHours { get; set; } = false;

        public bool AssistiveTechnologySupport { get; set; } = false;

        [StringLength(300)]
        public string? AssistiveTechDetails { get; set; }

        [Required(ErrorMessage = "Required skills are required")]
        [StringLength(500)]
        public string RequiredSkills { get; set; } = string.Empty;

        [StringLength(300)]
        public string? EducationRequirement { get; set; }

        [Required(ErrorMessage = "Application deadline is required")]
        public DateTime ApplicationDeadline { get; set; }

        public int VacancyCount { get; set; } = 1;

        [StringLength(50)]
        public string? ExperienceLevel { get; set; } 

        [Required]
        [StringLength(30)]
        public string JobStatus { get; set; } = "Active"; 

        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public virtual ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    }
}
