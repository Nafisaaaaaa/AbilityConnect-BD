using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SDP1.Models
{
   
    public class TrainingProgram
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        [Required(ErrorMessage = "Training title is required")]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Training category is required")]
        [StringLength(100)]
        public string TrainingCategory { get; set; } = "Computer Training"; 
        [Required(ErrorMessage = "Skills covered is required")]
        [StringLength(500)]
        public string SkillsCovered { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duration is required")]
        [StringLength(100)]
        public string Duration { get; set; } = "3 Months";

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Registration deadline is required")]
        [DataType(DataType.Date)]
        public DateTime RegistrationDeadline { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery mode is required")]
        [StringLength(50)]
        public string DeliveryMode { get; set; } = "Online"; 

        [Required(ErrorMessage = "Eligibility criteria is required")]
        [StringLength(500)]
        public string Eligibility { get; set; } = "Open to persons with disabilities";

        public int? MaxParticipants { get; set; }

        [Required(ErrorMessage = "Contact information is required")]
        [StringLength(250)]
        public string ContactInfo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active"; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<TrainingRegistration> Registrations { get; set; } = new List<TrainingRegistration>();
    }

    public class TrainingRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainingProgramId { get; set; }

        [ForeignKey("TrainingProgramId")]
        public virtual TrainingProgram? TrainingProgram { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Registered"; 
    }

    
    public class Scholarship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        [Required(ErrorMessage = "Scholarship title is required")]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scholarship type is required")]
        [StringLength(100)]
        public string ScholarshipType { get; set; } = "Full Funding"; 
        [Required(ErrorMessage = "Field or subject of study is required")]
        [StringLength(150)]
        public string FieldOfStudy { get; set; } = "General";

        [Required(ErrorMessage = "Scholarship benefits are required")]
        [StringLength(1000)]
        public string Benefits { get; set; } = string.Empty;

        [Required(ErrorMessage = "Eligibility requirements are required")]
        [StringLength(1000)]
        public string Eligibility { get; set; } = string.Empty;

        [Required(ErrorMessage = "Required qualifications are required")]
        [StringLength(500)]
        public string RequiredQualifications { get; set; } = string.Empty;

        [Required(ErrorMessage = "Application deadline is required")]
        [DataType(DataType.Date)]
        public DateTime ApplicationDeadline { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "Location or mode is required")]
        [StringLength(200)]
        public string Location { get; set; } = "Online / Bangladesh";

        [Required(ErrorMessage = "Contact information is required")]
        [StringLength(250)]
        public string ContactInfo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ScholarshipApplication> Applications { get; set; } = new List<ScholarshipApplication>();
    }

    public class ScholarshipApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ScholarshipId { get; set; }

        [ForeignKey("ScholarshipId")]
        public virtual Scholarship? Scholarship { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Highest qualification is required")]
        [StringLength(100)]
        public string HighestQualification { get; set; } = string.Empty;

        [Required(ErrorMessage = "Institution name is required")]
        [StringLength(200)]
        public string Institution { get; set; } = string.Empty;

        [Required(ErrorMessage = "Field of study is required")]
        [StringLength(150)]
        public string FieldOfStudy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passing year is required")]
        [StringLength(20)]
        public string PassingYear { get; set; } = string.Empty;

        [StringLength(50)]
        public string? AcademicResult { get; set; } 
        [Required(ErrorMessage = "Technical or computer skills are required")]
        [StringLength(500)]
        public string TechnicalSkills { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ProfessionalSkills { get; set; }

        [StringLength(500)]
        public string? OtherSkills { get; set; }

        [Required(ErrorMessage = "Please explain your interest in this scholarship")]
        [StringLength(2000, MinimumLength = 20)]
        public string Motivation { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? RelevantExperience { get; set; }

        [Required(ErrorMessage = "CV file is required")]
        [StringLength(500)]
        public string CVFilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string CVOriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; 

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    
    public class AwarenessEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        [Required(ErrorMessage = "Event title is required")]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [StringLength(50)]
        public string StartTime { get; set; } = "10:00 AM";

        [Required(ErrorMessage = "End time is required")]
        [StringLength(50)]
        public string EndTime { get; set; } = "01:00 PM";

        [Required(ErrorMessage = "Location or platform link is required")]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event type is required")]
        [StringLength(100)]
        public string EventType { get; set; } = "Disability Awareness"; 
        [Required(ErrorMessage = "Eligibility is required")]
        [StringLength(500)]
        public string Eligibility { get; set; } = "Open to everyone interested in disability empowerment";

        [Required(ErrorMessage = "Registration deadline is required")]
        [DataType(DataType.Date)]
        public DateTime RegistrationDeadline { get; set; }

        [Required(ErrorMessage = "Contact information is required")]
        [StringLength(250)]
        public string ContactInfo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active"; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    }

    public class EventRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AwarenessEventId { get; set; }

        [ForeignKey("AwarenessEventId")]
        public virtual AwarenessEvent? AwarenessEvent { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Registered"; 
    }

    
    public class SavedOpportunity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        [Required]
        [StringLength(50)]
        public string OpportunityType { get; set; } = "Training"; 

        [Required]
        public int OpportunityId { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public static class OpportunityTypes
    {
        public const string Training = "Training";
        public const string Scholarship = "Scholarship";
        public const string Event = "Event";
    }

    
    public class TrainingListViewModel
    {
        public List<TrainingProgram> Trainings { get; set; } = new();
        public string? SearchKeyword { get; set; }
        public string? Category { get; set; }
        public string? DeliveryMode { get; set; }
        public string? SortOrder { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public List<string> AvailableCategories { get; set; } = new();
        public HashSet<int> RegisteredTrainingIds { get; set; } = new();
        public HashSet<int> SavedTrainingIds { get; set; } = new();
    }

    public class TrainingDetailsViewModel
    {
        public TrainingProgram Training { get; set; } = null!;
        public bool IsRegistered { get; set; }
        public bool IsSaved { get; set; }
        public bool IsOrganizationOwner { get; set; }
        public int RegistrationCount { get; set; }
    }

    public class ScholarshipListViewModel
    {
        public List<Scholarship> Scholarships { get; set; } = new();
        public string? SearchKeyword { get; set; }
        public string? ScholarshipType { get; set; }
        public string? FieldOfStudy { get; set; }
        public string? SortOrder { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public List<string> AvailableTypes { get; set; } = new();
        public HashSet<int> AppliedScholarshipIds { get; set; } = new();
        public HashSet<int> SavedScholarshipIds { get; set; } = new();
    }

    public class ScholarshipDetailsViewModel
    {
        public Scholarship Scholarship { get; set; } = null!;
        public bool HasApplied { get; set; }
        public string? ApplicationStatus { get; set; }
        public bool IsSaved { get; set; }
        public bool IsOrganizationOwner { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class ScholarshipApplicationInputModel
    {
        [Required]
        public int ScholarshipId { get; set; }

        public string? ScholarshipTitle { get; set; }
        public string? OrganizationName { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Highest Qualification is required")]
        public string HighestQualification { get; set; } = string.Empty;

        [Required(ErrorMessage = "Institution is required")]
        public string Institution { get; set; } = string.Empty;

        [Required(ErrorMessage = "Field of Study is required")]
        public string FieldOfStudy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passing Year is required")]
        public string PassingYear { get; set; } = string.Empty;

        public string? AcademicResult { get; set; }

        [Required(ErrorMessage = "Technical/Computer skills are required")]
        public string TechnicalSkills { get; set; } = string.Empty;

        public string? ProfessionalSkills { get; set; }
        public string? OtherSkills { get; set; }

        [Required(ErrorMessage = "Please explain your interest in this scholarship")]
        [StringLength(2000, MinimumLength = 20)]
        public string Motivation { get; set; } = string.Empty;

        public string? RelevantExperience { get; set; }

        [Required(ErrorMessage = "Please upload your CV (PDF, DOC, DOCX)")]
        public IFormFile? CVFile { get; set; }
    }

    public class EventListViewModel
    {
        public List<AwarenessEvent> Events { get; set; } = new();
        public string? SearchKeyword { get; set; }
        public string? EventType { get; set; }
        public string? SortOrder { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public List<string> AvailableEventTypes { get; set; } = new();
        public HashSet<int> RegisteredEventIds { get; set; } = new();
        public HashSet<int> SavedEventIds { get; set; } = new();
    }

    public class EventDetailsViewModel
    {
        public AwarenessEvent Event { get; set; } = null!;
        public bool IsRegistered { get; set; }
        public bool IsSaved { get; set; }
        public bool IsOrganizationOwner { get; set; }
        public int RegistrationCount { get; set; }
    }

    public class SavedOpportunitiesViewModel
    {
        public List<TrainingProgram> SavedTrainings { get; set; } = new();
        public List<Scholarship> SavedScholarships { get; set; } = new();
        public List<AwarenessEvent> SavedEvents { get; set; } = new();
        public string CurrentTab { get; set; } = "All";
    }

    public class MyRegistrationsViewModel
    {
        public List<TrainingRegistration> TrainingRegistrations { get; set; } = new();
        public List<ScholarshipApplication> ScholarshipApplications { get; set; } = new();
        public List<EventRegistration> EventRegistrations { get; set; } = new();
    }
}
