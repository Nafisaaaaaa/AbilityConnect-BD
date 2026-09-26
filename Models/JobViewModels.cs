using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SDP1.Models
{
    public class JobCreateViewModel
    {
        [Required(ErrorMessage = "Job title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Job title must be between 3 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a job type")]
        public string JobType { get; set; } = "Full-time";

        [Required(ErrorMessage = "Please select workplace/remote option")]
        public string WorkplaceType { get; set; } = "Onsite";

        [Required(ErrorMessage = "Job location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Salary format cannot exceed 100 characters")]
        public string? Salary { get; set; }

        [StringLength(500)]
        public string? AccessibilityFeatures { get; set; }

        public bool FlexibleWorkingHours { get; set; } = false;

        public bool AssistiveTechnologySupport { get; set; } = false;

        [StringLength(300)]
        public string? AssistiveTechDetails { get; set; }

        [Required(ErrorMessage = "Required skills are required")]
        [StringLength(500, ErrorMessage = "Required skills cannot exceed 500 characters")]
        public string RequiredSkills { get; set; } = string.Empty;

        [StringLength(300)]
        public string? EducationRequirement { get; set; }

        [Required(ErrorMessage = "Application deadline is required")]
        [DataType(DataType.Date)]
        public DateTime ApplicationDeadline { get; set; } = DateTime.Today.AddMonths(1);

        [Range(1, 100, ErrorMessage = "Vacancy count must be between 1 and 100")]
        public int VacancyCount { get; set; } = 1;

        public string? ExperienceLevel { get; set; } = "Freshers Welcome";

        public bool IsPublished { get; set; } = true;
    }

    public class JobEditViewModel : JobCreateViewModel
    {
        public int Id { get; set; }
        public string JobStatus { get; set; } = "Active";
    }

    public class JobApplyViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string WorkplaceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your full name is required")]
        [StringLength(100)]
        public string ApplicantFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string ApplicantEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string ApplicantPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please upload your resume (PDF, DOC, DOCX up to 5MB)")]
        public IFormFile? ResumeFile { get; set; }

        [StringLength(2000, ErrorMessage = "Cover letter cannot exceed 2000 characters")]
        public string? CoverLetter { get; set; }

        [StringLength(1000, ErrorMessage = "Accommodation details cannot exceed 1000 characters")]
        public string? AccommodationRequested { get; set; }
    }

    public class JobListViewModel
    {
        public List<Job> Jobs { get; set; } = new List<Job>();

        public string? SearchKeyword { get; set; }
        public string? JobType { get; set; }
        public string? WorkplaceType { get; set; }
        public string? Location { get; set; }
        public string? AccessibilityFeature { get; set; }
        public bool? FlexibleHoursOnly { get; set; }
        public bool? AssistiveTechOnly { get; set; }
        public string? SortOrder { get; set; } = "newest";

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalJobs { get; set; } = 0;
        public int PageSize { get; set; } = 9;

        public HashSet<int> AppliedJobIds { get; set; } = new HashSet<int>();
        public HashSet<int> SavedJobIds { get; set; } = new HashSet<int>();
        public Dictionary<int, string> ApplicationStatuses { get; set; } = new Dictionary<int, string>();

        public List<string> AvailableLocations { get; set; } = new List<string>();
        public List<string> AvailableJobTypes { get; set; } = new List<string>();
        public List<string> AvailableWorkplaceTypes { get; set; } = new List<string>();
    }

    public class JobDetailsViewModel
    {
        public Job Job { get; set; } = null!;
        public bool HasApplied { get; set; } = false;
        public JobApplication? Application { get; set; }
        public bool IsSaved { get; set; } = false;
        public bool IsOwner { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public bool IsDisabilityUser { get; set; } = false;
    }

    public class OrgJobItemViewModel
    {
        public Job Job { get; set; } = null!;
        public int ApplicationCount { get; set; }
        public int PendingCount { get; set; }
        public int ShortlistedCount { get; set; }
    }

    public class OrgJobManageViewModel
    {
        public List<OrgJobItemViewModel> Jobs { get; set; } = new List<OrgJobItemViewModel>();
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int TotalApplicants { get; set; }
    }

    public class JobApplicantsViewModel
    {
        public Job Job { get; set; } = null!;
        public List<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public string? StatusFilter { get; set; }
    }
}
