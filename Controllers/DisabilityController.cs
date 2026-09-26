using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System.Linq;

namespace SDP1.Controllers
{
    public class DisabilityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisabilityController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsDisability()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Disability";
        }

        private void SetUserInfo()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "User";
            ViewBag.DisabilityType = HttpContext.Session.GetString("DisabilityType") ?? "Not Specified";
            ViewBag.ProfilePicture = HttpContext.Session.GetString("ProfilePicture") ?? "/images/default-avatar.png";
        }

       
        public IActionResult Dashboard()
        {
            if (!IsDisability())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var userId = HttpContext.Session.GetInt32("UserId");

            ViewBag.JobCount = _context.Jobs.Count(j => j.IsPublished && j.JobStatus == "Active");
            ViewBag.VolunteerCount = _context.Volunteers.Count(v => v.IsVerified);
            ViewBag.DoctorCount = _context.HealthcareAppointments.Count(a => a.DisabilityUserId == userId && (a.Status == "Pending" || a.Status == "Confirmed"));
            ViewBag.ScholarshipCount = _context.Scholarships.Count(s => s.Status == "Active");
            ViewBag.TrainingCount = _context.TrainingPrograms.Count(t => t.Status == "Active");
            ViewBag.EventCount = _context.AwarenessEvents.Count(e => e.Status == "Active");
            ViewBag.LearningCount = _context.LearningVideos.Count(v => v.IsPublished);

            var upcomingAppointments = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .Where(a => a.DisabilityUserId == userId && (a.Status == "Pending" || a.Status == "Confirmed"))
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .ToList();
            ViewBag.UpcomingAppointments = upcomingAppointments;

            return View();
        }

        
        public IActionResult RecommendedJobs()
        {
            if (!IsDisability())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = _context.DisabilityUsers.Find(userId);
            var disabilityType = user?.DisabilityType ?? "";
            var skills = user?.Skills ?? "";

            var jobs = _context.Jobs
                .Include(j => j.Organization)
                .Where(j => j.IsPublished && j.JobStatus == "Active")
                .OrderByDescending(j => j.CreatedAt)
                .Take(12)
                .ToList();

            ViewBag.UserDisability = disabilityType;
            ViewBag.UserSkills = skills;

            var appliedIds = _context.JobApplications
                .Where(ja => ja.DisabilityUserId == userId)
                .Select(ja => ja.JobId)
                .ToHashSet();
            ViewBag.AppliedJobIds = appliedIds;

            return View(jobs);
        }

       
        public IActionResult NearbyNGOs()
        {
            if (!IsDisability())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = userId.HasValue ? _context.DisabilityUsers.Find(userId.Value) : null;

            (double lat, double lng) GetCityCoordinates(string? cityOrDistrict, int seedId)
            {
                var lower = (cityOrDistrict ?? "").ToLowerInvariant().Trim();
                double baseLat = 23.8103, baseLng = 90.4125;

                if (lower.Contains("chittagong") || lower.Contains("chattogram")) { baseLat = 22.3569; baseLng = 91.7832; }
                else if (lower.Contains("rajshahi")) { baseLat = 24.3745; baseLng = 88.6042; }
                else if (lower.Contains("khulna")) { baseLat = 22.8456; baseLng = 89.5403; }
                else if (lower.Contains("sylhet")) { baseLat = 24.8949; baseLng = 91.8687; }
                else if (lower.Contains("barisal") || lower.Contains("barishal")) { baseLat = 22.7010; baseLng = 90.3535; }
                else if (lower.Contains("rangpur")) { baseLat = 25.7439; baseLng = 89.2752; }
                else if (lower.Contains("mymensingh")) { baseLat = 24.7471; baseLng = 90.4203; }
                else if (lower.Contains("cumilla") || lower.Contains("comilla")) { baseLat = 23.4682; baseLng = 91.1788; }
                else if (lower.Contains("gazipur")) { baseLat = 23.9999; baseLng = 90.4203; }
                else if (lower.Contains("narayanganj")) { baseLat = 23.6238; baseLng = 90.5000; }

                double offsetLat = ((seedId * 17) % 19 - 9) * 0.0035;
                double offsetLng = ((seedId * 31) % 19 - 9) * 0.0035;
                return (baseLat + offsetLat, baseLng + offsetLng);
            }

            var orgs = _context.Organizations.ToList().Select(o =>
            {
                var (defaultLat, defaultLng) = GetCityCoordinates(!string.IsNullOrWhiteSpace(o.Address) ? o.Address : (o.City ?? o.Division), o.Id);
                return new NearbyNgoItemViewModel
                {
                    Id = o.Id,
                    Name = o.OrganizationName,
                    OrganizationType = o.OrganizationType,
                    Description = o.OrganizationDescription,
                    Address = o.Address,
                    City = o.City,
                    Division = o.Division,
                    ContactPerson = o.ContactPersonName,
                    Phone = !string.IsNullOrEmpty(o.ContactPersonPhone) ? o.ContactPersonPhone : o.PhoneNumber,
                    Email = o.Email,
                    Website = o.Website,
                    FacebookProfileLink = o.FacebookProfileLink,
                    Latitude = o.Latitude ?? defaultLat,
                    Longitude = o.Longitude ?? defaultLng
                };
            }).ToList();

            var volunteers = _context.Volunteers.Where(v => v.IsAvailable).ToList().Select(v =>
            {
                var (defaultLat, defaultLng) = GetCityCoordinates(v.City ?? v.Location, v.Id + 50);
                return new NearbyVolunteerItemViewModel
                {
                    Id = v.Id,
                    Name = v.FullName,
                    Area = !string.IsNullOrWhiteSpace(v.City) ? $"{v.City}, {v.District}" : v.Location,
                    City = v.City ?? "",
                    District = v.District ?? "",
                    SupportTypes = v.AssistanceTypes,
                    Skills = v.Skills,
                    Bio = v.Bio,
                    Phone = v.PhoneNumber,
                    Email = v.Email,
                    IsAvailable = v.IsAvailable,
                    Latitude = v.Latitude ?? defaultLat,
                    Longitude = v.Longitude ?? defaultLng
                };
            }).ToList();

            var model = new NearbyNGOsAndVolunteersViewModel
            {
                Organizations = orgs,
                Volunteers = volunteers,
                UserLocation = user?.Location ?? "Dhaka, Bangladesh",
                DefaultLat = user?.Latitude ?? 23.8103,
                DefaultLng = user?.Longitude ?? 90.4125
            };

            return View(model);
        }

        
        public IActionResult VolunteerSupport()
        {
            if (!IsDisability())
                return RedirectToAction("Login", "Account");

            return RedirectToAction("Index", "VolunteerSupport");
        }

      
        public IActionResult DoctorAppointments()
        {
            if (!IsDisability())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var userId = HttpContext.Session.GetInt32("UserId");
            var appointments = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .Where(a => a.DisabilityUserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            return View(appointments);
        }

    
        public IActionResult Scholarships()
        {
            return RedirectToAction("Scholarships", "Services");
        }

        
        public IActionResult AISuggestions()
        {
            if (!IsDisability())
                return RedirectToAction("Login", "Account");

            return RedirectToAction("Index", "AIRecommendation");
        }
    }
}