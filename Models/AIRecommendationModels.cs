using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SDP1.Models
{
    public class AIRecommendationRequestViewModel
    {
        [Required(ErrorMessage = "Please enter what you are looking for.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Please describe what support or opportunity you need (at least 3 characters).")]
        public string Query { get; set; } = string.Empty;
    }

    public class AIRecommendationResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<AIRecommendedItemViewModel> Recommendations { get; set; } = new();
        public bool IsAiGenerated { get; set; } = false;
        public string? NoticeMessage { get; set; }
        public string? DisabilityContext { get; set; }
        public int TotalMatchesFound { get; set; }
    }

    public class AIRecommendedItemViewModel
    {
        public string ItemType { get; set; } = string.Empty; 
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OrganizationOrProvider { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = "bg-primary";
        public string IconClass { get; set; } = "fas fa-check-circle";
        public string? Location { get; set; }
        public string? ExtraDetails { get; set; }
    }

    public class CandidateOpportunity
    {
        public string Type { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProviderOrOrg { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryOrField { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = "bg-primary";
        public string IconClass { get; set; } = "fas fa-star";
    }
}
