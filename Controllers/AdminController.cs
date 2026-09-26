using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDP1.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Admin";
        }

   
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            
            ViewBag.TotalUsers = _context.DisabilityUsers.Count();

            ViewBag.TotalOrganizations = _context.Organizations.Count(o => o.IsVerified);

            
            ViewBag.TotalVolunteers = _context.Volunteers.Count(v => v.IsVerified);

           
            ViewBag.PendingVolunteers = _context.Volunteers.Count(v => !v.IsVerified);
            ViewBag.PendingOrganizations = _context.Organizations.Count(o => !o.IsVerified);

    
            ViewBag.CommunityPostsCount = _context.CommunityPosts.Count(p => !p.IsDeleted);

            
            var activities = new List<ActivityItem>();

            
            activities.AddRange(_context.DisabilityUsers
                .OrderByDescending(u => u.RegisteredAt)
                .Take(5)
                .ToList()
                .Select(u => new ActivityItem
                {
                    User = u.FullName,
                    Action = "Registered as Disability User",
                    Status = "Active",
                    StatusClass = "bg-success",
                    Date = u.RegisteredAt,
                    UserId = u.Id,
                    Type = "Disability"
                }));

            
            activities.AddRange(_context.Volunteers
                .OrderByDescending(v => v.RegisteredAt)
                .Take(5)
                .ToList()
                .Select(v => new ActivityItem
                {
                    User = v.FullName,
                    Action = "Registered as Volunteer",
                    Status = v.IsVerified ? "Verified" : "Pending",
                    StatusClass = v.IsVerified ? "bg-success" : "bg-warning",
                    Date = v.RegisteredAt,
                    UserId = v.Id,
                    Type = "Volunteer"
                }));

            
            activities.AddRange(_context.Organizations
                .OrderByDescending(o => o.RegisteredAt)
                .Take(5)
                .ToList()
                .Select(o => new ActivityItem
                {
                    User = o.OrganizationName,
                    Action = "Registered as Organization",
                    Status = o.IsVerified ? "Verified" : "Pending",
                    StatusClass = o.IsVerified ? "bg-success" : "bg-warning",
                    Date = o.RegisteredAt,
                    UserId = o.Id,
                    Type = "Organization"
                }));

          
            ViewBag.RecentActivities = activities
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToList();

            return View();
        }


       
        public IActionResult ManageUsers()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var users = _context.DisabilityUsers
                .OrderByDescending(u => u.RegisteredAt)
                .ToList();

            return View(users);
        }

        public IActionResult UserProfile(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var user = _context.DisabilityUsers.Find(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

       
        public IActionResult ManageVolunteers()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var volunteers = _context.Volunteers
                .OrderByDescending(v => v.RegisteredAt)
                .ToList();

            return View(volunteers);
        }

        public IActionResult VolunteerProfile(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null)
                return NotFound();

            return View(volunteer);
        }
        public IActionResult DeleteVolunteer(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null)
                return NotFound();

            return View(volunteer);
        }

       
        [HttpPost, ActionName("DeleteVolunteer")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteVolunteerConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null)
                return NotFound();

            _context.Volunteers.Remove(volunteer);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Volunteer deleted successfully.";
            return RedirectToAction("ManageVolunteers");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyVolunteer(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null)
                return NotFound();

            volunteer.IsVerified = true;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Volunteer verified successfully.";
            return RedirectToAction("ManageVolunteers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnverifyVolunteer(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null)
                return NotFound();

            volunteer.IsVerified = false;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Volunteer unverified.";
            return RedirectToAction("ManageVolunteers");
        }

    
        public IActionResult VerifyOrganizations()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var orgs = _context.Organizations
                .OrderByDescending(o => o.RegisteredAt)
                .ToList();

            return View(orgs);
        }


        public IActionResult OrganizationProfile(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var org = _context.Organizations.Find(id);
            if (org == null)
                return NotFound();

            return View(org);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOrganization(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var org = _context.Organizations.Find(id);
            if (org == null)
                return NotFound();

            org.IsVerified = true;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Organization verified successfully.";
            return RedirectToAction("VerifyOrganizations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnverifyOrganization(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var org = _context.Organizations.Find(id);
            if (org == null)
                return NotFound();

            org.IsVerified = false;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Organization unverified.";
            return RedirectToAction("VerifyOrganizations");
        }

      
        public IActionResult JobsAndPrograms()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var jobs = _context.Jobs
                .Include(j => j.Organization)
                .Include(j => j.JobApplications)
                .OrderByDescending(j => j.CreatedAt)
                .ToList();

            return View(jobs);
        }

        
        public IActionResult HealthcareProviders(string? type)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            IQueryable<HealthcareProvider> query = _context.HealthcareProviders
                .Include(p => p.Organization)
                .Include(p => p.Appointments);

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
            {
                query = query.Where(p => p.ProviderType == type);
            }

            var providers = query.OrderByDescending(p => p.CreatedAt).ToList();

            ViewBag.CurrentType = type ?? "All";
            ViewBag.TotalCount = _context.HealthcareProviders.Count();
            ViewBag.TotalAppointments = _context.HealthcareAppointments.Count();

            return View(providers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteHealthcareProvider(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var provider = _context.HealthcareProviders.Find(id);
            if (provider == null) return NotFound();

            _context.HealthcareProviders.Remove(provider);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Healthcare provider '{provider.Name}' removed successfully.";
            return RedirectToAction(nameof(HealthcareProviders));
        }

        [HttpGet]
        public IActionResult DoctorAppointments(string? statusFilter, int? providerId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            IQueryable<HealthcareAppointment> query = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .Include(a => a.DisabilityUser);

            if (providerId.HasValue && providerId.Value > 0)
            {
                query = query.Where(a => a.HealthcareProviderId == providerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            var appointments = query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.CreatedAt)
                .ToList();

            ViewBag.CurrentStatusFilter = statusFilter ?? "All";
            ViewBag.SelectedProviderId = providerId;
            ViewBag.AllProviders = _context.HealthcareProviders.OrderBy(p => p.Name).ToList();

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDoctorAppointmentStatus(int id, string status, string? doctorNotes)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var appointment = _context.HealthcareAppointments.Find(id);
            if (appointment != null)
            {
                appointment.Status = status;
                if (!string.IsNullOrEmpty(doctorNotes))
                {
                    appointment.DoctorNotes = doctorNotes;
                }
                appointment.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Appointment status updated to '{status}'.";
            }

            return RedirectToAction(nameof(DoctorAppointments));
        }
        public IActionResult CommunityReports(string? type = null, string? status = null, string? search = null)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return RedirectToAction("Moderation", "Community");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCommunityReportStatus(int id, string status, string? adminNotes)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var report = await _context.CommunityReports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = status;
            report.AdminNotes = adminNotes?.Trim();
            report.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Report #{id} marked as '{status}'.";
            return RedirectToAction(nameof(CommunityReports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCommunityReport(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var report = await _context.CommunityReports.FindAsync(id);
            if (report == null) return NotFound();

            _context.CommunityReports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Community report deleted successfully.";
            return RedirectToAction(nameof(CommunityReports));
        }

        
        public IActionResult AccessibilityPlaces()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var places = _context.AccessibilityPlaces
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(places);
        }

        [HttpGet]
        public IActionResult CreateAccessibilityPlace()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var model = new AccessibilityPlaceFormViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAccessibilityPlace(AccessibilityPlaceFormViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";
                return View(model);
            }

            var place = new AccessibilityPlace
            {
                PlaceName = model.PlaceName.Trim(),
                PlaceType = model.PlaceType,
                Description = model.Description.Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                District = model.District.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                WheelchairRamp = model.WheelchairRamp,
                Elevator = model.Elevator,
                AccessibleToilet = model.AccessibleToilet,
                AccessibleParking = model.AccessibleParking,
                AccessibilityScore = model.AccessibilityScore,
                CreatedAt = DateTime.UtcNow
            };

            _context.AccessibilityPlaces.Add(place);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Place '{place.PlaceName}' added successfully.";
            return RedirectToAction(nameof(AccessibilityPlaces));
        }

        [HttpGet]
        public IActionResult EditAccessibilityPlace(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var place = _context.AccessibilityPlaces.Find(id);
            if (place == null) return NotFound();

            var model = new AccessibilityPlaceFormViewModel
            {
                Id = place.Id,
                PlaceName = place.PlaceName,
                PlaceType = place.PlaceType,
                Description = place.Description,
                Address = place.Address,
                City = place.City,
                District = place.District,
                Latitude = place.Latitude,
                Longitude = place.Longitude,
                WheelchairRamp = place.WheelchairRamp,
                Elevator = place.Elevator,
                AccessibleToilet = place.AccessibleToilet,
                AccessibleParking = place.AccessibleParking,
                AccessibilityScore = place.AccessibilityScore
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAccessibilityPlace(AccessibilityPlaceFormViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!model.Id.HasValue) return NotFound();
            var place = _context.AccessibilityPlaces.Find(model.Id.Value);
            if (place == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";
                return View(model);
            }

            place.PlaceName = model.PlaceName.Trim();
            place.PlaceType = model.PlaceType;
            place.Description = model.Description.Trim();
            place.Address = model.Address.Trim();
            place.City = model.City.Trim();
            place.District = model.District.Trim();
            place.Latitude = model.Latitude;
            place.Longitude = model.Longitude;
            place.WheelchairRamp = model.WheelchairRamp;
            place.Elevator = model.Elevator;
            place.AccessibleToilet = model.AccessibleToilet;
            place.AccessibleParking = model.AccessibleParking;
            place.AccessibilityScore = model.AccessibilityScore;
            place.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Place '{place.PlaceName}' updated successfully.";
            return RedirectToAction(nameof(AccessibilityPlaces));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAccessibilityPlace(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var place = _context.AccessibilityPlaces.Find(id);
            if (place == null) return NotFound();

            _context.AccessibilityPlaces.Remove(place);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Place '{place.PlaceName}' deleted successfully.";
            return RedirectToAction(nameof(AccessibilityPlaces));
        }

        public IActionResult CommunityModeration(string? tab)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return RedirectToAction("Moderation", "Community", new { tab });
        }



        public IActionResult LearningHub(string? category, string? search, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var pageSize = 15;
            var query = _context.LearningVideos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(v => v.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(v => v.Title.ToLower().Contains(s) || v.Description.ToLower().Contains(s));
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var videos = query
                .OrderByDescending(v => v.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Category = category;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Categories = LearningCategories.All;

            return View(videos);
        }

        [HttpGet]
        public IActionResult CreateLearningVideo()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var model = new LearningVideoFormViewModel
            {
                Category = LearningCategories.Braille,
                IsPublished = true,
                Duration = "15 mins"
            };

            ViewBag.Categories = LearningCategories.All;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateLearningVideo(LearningVideoFormViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";
            ViewBag.Categories = LearningCategories.All;

            if (!YouTubeHelper.IsValidYouTubeUrl(model.YouTubeUrl))
            {
                ModelState.AddModelError("YouTubeUrl", "Please enter a valid YouTube video URL (e.g., https://www.youtube.com/watch?v=... or https://youtu.be/...).");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var video = new LearningVideo
            {
                Title = model.Title.Trim(),
                Category = model.Category,
                Description = model.Description.Trim(),
                YouTubeUrl = model.YouTubeUrl.Trim(),
                Duration = model.Duration?.Trim(),
                IsPublished = model.IsPublished,
                CreatedAt = DateTime.UtcNow
            };

            _context.LearningVideos.Add(video);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Lesson '{video.Title}' added successfully to Learning Hub.";
            return RedirectToAction(nameof(LearningHub));
        }

        [HttpGet]
        public IActionResult EditLearningVideo(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            var video = _context.LearningVideos.Find(id);
            if (video == null) return NotFound();

            var model = new LearningVideoFormViewModel
            {
                Id = video.Id,
                Title = video.Title,
                Category = video.Category,
                Description = video.Description,
                YouTubeUrl = video.YouTubeUrl,
                Duration = video.Duration,
                IsPublished = video.IsPublished
            };

            ViewBag.Categories = LearningCategories.All;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLearningVideo(int id, LearningVideoFormViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Administrator";
            ViewBag.Categories = LearningCategories.All;

            var video = _context.LearningVideos.Find(id);
            if (video == null) return NotFound();

            if (!YouTubeHelper.IsValidYouTubeUrl(model.YouTubeUrl))
            {
                ModelState.AddModelError("YouTubeUrl", "Please enter a valid YouTube video URL (e.g., https://www.youtube.com/watch?v=... or https://youtu.be/...).");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            video.Title = model.Title.Trim();
            video.Category = model.Category;
            video.Description = model.Description.Trim();
            video.YouTubeUrl = model.YouTubeUrl.Trim();
            video.Duration = model.Duration?.Trim();
            video.IsPublished = model.IsPublished;
            video.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Lesson '{video.Title}' updated successfully.";
            return RedirectToAction(nameof(LearningHub));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLearningVideo(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var video = _context.LearningVideos.Find(id);
            if (video == null) return NotFound();

            _context.LearningVideos.Remove(video);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Lesson '{video.Title}' deleted from Learning Hub.";
            return RedirectToAction(nameof(LearningHub));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TogglePublishLearningVideo(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var video = _context.LearningVideos.Find(id);
            if (video == null) return NotFound();

            video.IsPublished = !video.IsPublished;
            video.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Lesson '{video.Title}' is now {(video.IsPublished ? "Published" : "Hidden")}.";
            return RedirectToAction(nameof(LearningHub));
        }
    }
}