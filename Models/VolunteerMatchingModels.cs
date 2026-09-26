using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class VolunteerSupportRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestedByUserId { get; set; }

        [ForeignKey("RequestedByUserId")]
        public virtual DisabilityUser? RequestedByUser { get; set; }

        [Required]
        [StringLength(50)]
        public string AssistanceType { get; set; } = string.Empty; 

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestTime { get; set; } = string.Empty; 

        [Required]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Required]
        [StringLength(20)]
        public string Urgency { get; set; } = "Standard"; 

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; 

        public int? AssignedVolunteerId { get; set; }

        [ForeignKey("AssignedVolunteerId")]
        public virtual Volunteer? AssignedVolunteer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [StringLength(300)]
        public string? CancellationReason { get; set; }

        
        public virtual ICollection<VolunteerChatMessage> ChatMessages { get; set; } = new List<VolunteerChatMessage>();
    }

    public class VolunteerChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VolunteerSupportRequestId { get; set; }

        [ForeignKey("VolunteerSupportRequestId")]
        public virtual VolunteerSupportRequest? SupportRequest { get; set; }

        [Required]
        public int SenderUserId { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderRole { get; set; } = string.Empty; 

        [Required]
        [StringLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        [StringLength(1500)]
        public string MessageText { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }

    public class VolunteerSupportRequestCreateViewModel
    {
        [Required(ErrorMessage = "Please select an assistance type.")]
        public string AssistanceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a request title.")]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe the assistance needed in detail.")]
        [StringLength(1000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select the date assistance is needed.")]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Please specify preferred time.")]
        [StringLength(50)]
        public string RequestTime { get; set; } = "10:00 AM";

        [Required(ErrorMessage = "Please provide the specific address or venue.")]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify city.")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify district.")]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Required]
        public string Urgency { get; set; } = "Standard";

        public List<Volunteer> MatchingVolunteers { get; set; } = new();
    }

    public class VolunteerSupportRequestListViewModel
    {
        public List<VolunteerSupportRequest> Requests { get; set; } = new();
        public string? StatusFilter { get; set; }
        public int PendingCount { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class VolunteerNearbyRequestsViewModel
    {
        public List<VolunteerSupportRequest> AvailableRequests { get; set; } = new();
        public string? CityFilter { get; set; }
        public string? AssistanceTypeFilter { get; set; }
        public string? UrgencyFilter { get; set; }
        public Volunteer CurrentVolunteer { get; set; } = new();
        public string GoogleMapsApiKey { get; set; } = string.Empty;
        public bool HasGoogleMapsKey => !string.IsNullOrWhiteSpace(GoogleMapsApiKey);
    }

    public class VolunteerAssignedTasksViewModel
    {
        public List<VolunteerSupportRequest> ActiveTasks { get; set; } = new();
        public int InProgressCount { get; set; }
        public int AcceptedCount { get; set; }
    }

    public class VolunteerHistoryViewModel
    {
        public List<VolunteerSupportRequest> PastRequests { get; set; } = new();
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class VolunteerChatViewModel
    {
        public VolunteerSupportRequest Request { get; set; } = new();
        public List<VolunteerChatMessage> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public string CurrentUserRole { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        public string OtherPartyRole { get; set; } = string.Empty;
        public string? OtherPartyPhone { get; set; }
    }
}
