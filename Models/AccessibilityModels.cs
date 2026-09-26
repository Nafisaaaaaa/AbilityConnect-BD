using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SDP1.Models
{
    public class AccessibilityPlace
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string PlaceName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PlaceType { get; set; } = string.Empty; 

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool WheelchairRamp { get; set; } = false;
        public bool Elevator { get; set; } = false;
        public bool AccessibleToilet { get; set; } = false;
        public bool AccessibleParking { get; set; } = false;

        [Range(0, 100)]
        public int AccessibilityScore { get; set; } = 50;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

    }

    public class AccessibilityListViewModel
    {
        public List<AccessibilityPlace> Places { get; set; } = new();
        public string? SearchKeyword { get; set; }
        public string? PlaceType { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public bool? WheelchairRamp { get; set; }
        public bool? Elevator { get; set; }
        public bool? AccessibleToilet { get; set; }
        public bool? AccessibleParking { get; set; }
        public string? SortOrder { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int PageSize { get; set; } = 9;

        public List<string> AvailableCities { get; set; } = new();
        public List<string> AvailableDistricts { get; set; } = new();
        public List<string> AvailableTypes { get; set; } = new();
    }

    public class AccessibilityPlaceDetailsViewModel
    {
        public AccessibilityPlace Place { get; set; } = new();
        public bool IsDisabilityUser { get; set; }
        public bool IsAdmin { get; set; }
        public int? CurrentUserId { get; set; }
    }

    public class AccessibilityPlaceFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(150)]
        public string PlaceName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PlaceType { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public bool WheelchairRamp { get; set; }
        public bool Elevator { get; set; }
        public bool AccessibleToilet { get; set; }
        public bool AccessibleParking { get; set; }

        [Range(0, 100)]
        public int AccessibilityScore { get; set; } = 50;
    }
}
