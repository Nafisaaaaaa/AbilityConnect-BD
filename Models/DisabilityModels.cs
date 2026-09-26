using System.Collections.Generic;

namespace SDP1.Models
{
    public class NearbyNGOsAndVolunteersViewModel
    {
        public List<NearbyNgoItemViewModel> Organizations { get; set; } = new();
        public List<NearbyVolunteerItemViewModel> Volunteers { get; set; } = new();
        public string? UserLocation { get; set; }
        public double DefaultLat { get; set; } = 23.8103; 
        public double DefaultLng { get; set; } = 90.4125;
    }

    public class NearbyNgoItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OrganizationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? FacebookProfileLink { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class NearbyVolunteerItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? SupportTypes { get; set; }
        public string? Skills { get; set; }
        public string? Bio { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
