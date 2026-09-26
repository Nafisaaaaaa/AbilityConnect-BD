using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SDP1.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public string? Role { get; set; }
        public bool RememberMe { get; set; }
    }

    public class DisabilityRegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Disability type is required")]
        public string? DisabilityType { get; set; }

        public string? OtherDisabilityType { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string? Location { get; set; }

        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Interests { get; set; }

        public string? ProfilePicture { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }

        public string? DisabilityCertificate { get; set; }
        public IFormFile? DisabilityCertificateFile { get; set; }
    }

    public class VolunteerRegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string? Location { get; set; }

        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Skills are required")]
        public string? Skills { get; set; }

        public string? Availability { get; set; }
        public string? Experience { get; set; }
        public string? LanguagesSpoken { get; set; }

        public string? ProfilePicture { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
    }

    public class OrganizationRegisterViewModel
    {
        [Required(ErrorMessage = "Organization name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string? OrganizationName { get; set; }

        [Required(ErrorMessage = "Organization type is required")]
        public string? OrganizationType { get; set; }

        public string? OtherOrganizationType { get; set; }

        [Required(ErrorMessage = "Organization description is required")]
        [StringLength(500, MinimumLength = 10)]
        public string? OrganizationDescription { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Division { get; set; }

        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        public string? Website { get; set; }
        public string? FacebookProfileLink { get; set; }

        [Required(ErrorMessage = "Contact person name is required")]
        public string? ContactPersonName { get; set; }

        [Required(ErrorMessage = "Contact person designation is required")]
        public string? ContactPersonDesignation { get; set; }

        [Required(ErrorMessage = "Contact person phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? ContactPersonPhone { get; set; }

        [Required(ErrorMessage = "Contact person email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? ContactPersonEmail { get; set; }

        public string? Logo { get; set; }
        public IFormFile? LogoFile { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime? RegisteredAt { get; set; }
    }

    public class AdminLoginViewModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}