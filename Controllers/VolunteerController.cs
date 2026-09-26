using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

namespace SDP1.Controllers
{
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public VolunteerController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        #region Session / Role Helpers

        private bool IsVolunteer()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Volunteer";
        }

        private int? GetVolunteerId() => HttpContext.Session.GetInt32("UserId");

        private void SetUserInfo()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Volunteer";
            ViewBag.ProfilePicture = HttpContext.Session.GetString("ProfilePicture") ?? "/images/default-avatar.png";
        }

        #endregion

        
        public async Task<IActionResult> Dashboard()
        {
            if (!IsVolunteer())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var volunteerId = GetVolunteerId()!.Value;
            var volunteer = await _context.Volunteers.FindAsync(volunteerId);
            if (volunteer == null) return RedirectToAction("Login", "Account");

            var vCity = volunteer.City ?? volunteer.Location ?? "";

            
            var openHelpQuery = _context.VolunteerSupportRequests
                .Where(r => r.Status == "Pending" && r.AssignedVolunteerId == null);

            if (!string.IsNullOrWhiteSpace(vCity))
            {
                var cityTerm = vCity.Split(',')[0].Trim().ToLower();
                openHelpQuery = openHelpQuery.Where(r =>
                    r.City.ToLower().Contains(cityTerm) ||
                    r.District.ToLower().Contains(cityTerm) ||
                    r.Location.ToLower().Contains(cityTerm));
            }

            ViewBag.HelpRequestCount = await openHelpQuery.CountAsync();
            ViewBag.AssignedTaskCount = await _context.VolunteerSupportRequests
                .CountAsync(r => r.AssignedVolunteerId == volunteerId && (r.Status == "Accepted" || r.Status == "InProgress"));
            ViewBag.HistoryCount = await _context.VolunteerSupportRequests
                .CountAsync(r => r.AssignedVolunteerId == volunteerId && r.Status == "Completed");
            ViewBag.ChatRequestCount = ViewBag.AssignedTaskCount;
            ViewBag.IsAvailable = volunteer.IsAvailable;
            ViewBag.IsVerified = volunteer.IsVerified;
            ViewBag.VolunteerCity = vCity;

            var upcomingTasks = await _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.AssignedVolunteerId == volunteerId && (r.Status == "Accepted" || r.Status == "InProgress"))
                .OrderBy(r => r.RequestDate)
                .Take(5)
                .ToListAsync();

            ViewBag.UpcomingTasks = upcomingTasks;

            return View();
        }

        
        public async Task<IActionResult> NearbyHelpRequests(string? city, string? assistanceType, string? urgency)
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var volunteerId = GetVolunteerId()!.Value;
            var volunteer = await _context.Volunteers.FindAsync(volunteerId);
            if (volunteer == null) return RedirectToAction("Login", "Account");

            var query = _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == "Pending" && r.AssignedVolunteerId == null);

            var targetCity = !string.IsNullOrWhiteSpace(city) ? city : (volunteer.City ?? volunteer.Location?.Split(',')[0].Trim());
            if (!string.IsNullOrWhiteSpace(targetCity) && targetCity != "All")
            {
                var cTerm = targetCity.Trim().ToLower();
                query = query.Where(r =>
                    r.City.ToLower().Contains(cTerm) ||
                    r.District.ToLower().Contains(cTerm) ||
                    r.Location.ToLower().Contains(cTerm));
            }

            if (!string.IsNullOrWhiteSpace(assistanceType) && assistanceType != "All")
            {
                query = query.Where(r => r.AssistanceType == assistanceType);
            }

            if (!string.IsNullOrWhiteSpace(urgency) && urgency != "All")
            {
                query = query.Where(r => r.Urgency == urgency);
            }

            var availableRequests = await query
                .OrderByDescending(r => r.Urgency == "Emergency")
                .ThenByDescending(r => r.Urgency == "High")
                .ThenBy(r => r.RequestDate)
                .ToListAsync();

            
            foreach (var req in availableRequests)
            {
                if (!req.Latitude.HasValue || !req.Longitude.HasValue)
                {
                    if (req.RequestedByUser?.Latitude.HasValue == true && req.RequestedByUser?.Longitude.HasValue == true)
                    {
                        req.Latitude = req.RequestedByUser.Latitude;
                        req.Longitude = req.RequestedByUser.Longitude;
                    }
                    else
                    {
                        var (cLat, cLng) = GetCityCoordinates(req.City, req.District ?? req.Location);
                        req.Latitude = cLat;
                        req.Longitude = cLng;
                    }
                }
            }

            if (!volunteer.Latitude.HasValue || !volunteer.Longitude.HasValue)
            {
                var (vLat, vLng) = GetCityCoordinates(volunteer.City ?? volunteer.Location, volunteer.District);
                volunteer.Latitude = vLat;
                volunteer.Longitude = vLng;
            }

            var apiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            var viewModel = new VolunteerNearbyRequestsViewModel
            {
                AvailableRequests = availableRequests,
                CityFilter = targetCity,
                AssistanceTypeFilter = assistanceType,
                UrgencyFilter = urgency,
                CurrentVolunteer = volunteer,
                GoogleMapsApiKey = apiKey
            };

            return View(viewModel);
        }

        private static (double Lat, double Lng) GetCityCoordinates(string? city, string? district)
        {
            var target = (city ?? district ?? "").ToLowerInvariant();
            if (target.Contains("chittagong") || target.Contains("chattogram")) return (22.3569, 91.7832);
            if (target.Contains("sylhet")) return (24.8949, 91.8687);
            if (target.Contains("rajshahi")) return (24.3745, 88.6042);
            if (target.Contains("khulna")) return (22.8456, 89.5403);
            if (target.Contains("barishal") || target.Contains("barisal")) return (22.7010, 90.3535);
            if (target.Contains("rangpur")) return (25.7439, 89.2752);
            if (target.Contains("mymensingh")) return (24.7471, 90.4203);
            if (target.Contains("comilla") || target.Contains("cumilla")) return (23.4607, 91.1809);
            if (target.Contains("gazipur")) return (23.9999, 90.4203);
            if (target.Contains("narayanganj")) return (23.6238, 90.5000);
            return (23.8103, 90.4125); 
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");

            var volunteerId = GetVolunteerId()!.Value;
            var volunteer = await _context.Volunteers.FindAsync(volunteerId);

            if (volunteer == null) return NotFound();

            var request = await _context.VolunteerSupportRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            if (request.Status != "Pending" || request.AssignedVolunteerId != null)
            {
                TempData["ErrorMessage"] = "This request has already been accepted by another volunteer or is no longer pending.";
                return RedirectToAction(nameof(NearbyHelpRequests));
            }

            request.AssignedVolunteerId = volunteerId;
            request.Status = "Accepted";
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"You have accepted the support request '{request.Title}'. You can now coordinate via real-time chat!";
            return RedirectToAction(nameof(AssignedTasks));
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");

            var volunteerId = GetVolunteerId()!.Value;
            var request = await _context.VolunteerSupportRequests.FindAsync(requestId);

            if (request == null) return NotFound();

            
            if (request.AssignedVolunteerId == volunteerId)
            {
                request.AssignedVolunteerId = null;
                request.Status = "Pending";
                request.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["InfoMessage"] = "You have released this task. It is now open for other volunteers.";
            }

            return RedirectToAction(nameof(AssignedTasks));
        }

   
        public async Task<IActionResult> AssignedTasks()
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var volunteerId = GetVolunteerId()!.Value;

            var tasks = await _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.AssignedVolunteerId == volunteerId && (r.Status == "Accepted" || r.Status == "InProgress"))
                .OrderBy(r => r.RequestDate)
                .ToListAsync();

            var viewModel = new VolunteerAssignedTasksViewModel
            {
                ActiveTasks = tasks,
                AcceptedCount = tasks.Count(t => t.Status == "Accepted"),
                InProgressCount = tasks.Count(t => t.Status == "InProgress")
            };

            return View(viewModel);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskStatus(int requestId, string status)
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");

            var volunteerId = GetVolunteerId()!.Value;
            var request = await _context.VolunteerSupportRequests.FindAsync(requestId);

            if (request == null) return NotFound();

            if (request.AssignedVolunteerId != volunteerId)
            {
                TempData["ErrorMessage"] = "You are not authorized to update this task.";
                return RedirectToAction(nameof(AssignedTasks));
            }

            var validStatuses = new[] { "InProgress", "Completed" };
            if (!validStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid task status.";
                return RedirectToAction(nameof(AssignedTasks));
            }

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;

            if (status == "Completed")
            {
                request.CompletedAt = DateTime.UtcNow;
                TempData["SuccessMessage"] = $"Task '{request.Title}' has been marked as Completed! Thank you for your service.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Task '{request.Title}' is now marked as In Progress.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(status == "Completed" ? nameof(VolunteerHistory) : nameof(AssignedTasks));
        }

        
        public async Task<IActionResult> VolunteerHistory()
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var volunteerId = GetVolunteerId()!.Value;

            var pastTasks = await _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.AssignedVolunteerId == volunteerId && (r.Status == "Completed" || r.Status == "Cancelled"))
                .OrderByDescending(r => r.CompletedAt ?? r.UpdatedAt ?? r.CreatedAt)
                .ToListAsync();

            var viewModel = new VolunteerHistoryViewModel
            {
                PastRequests = pastTasks,
                CompletedCount = pastTasks.Count(t => t.Status == "Completed"),
                CancelledCount = pastTasks.Count(t => t.Status == "Cancelled")
            };

            return View(viewModel);
        }

        
        public IActionResult Chat(int requestId)
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");
            return RedirectToAction("Chat", "VolunteerSupport", new { id = requestId });
        }

        public async Task<IActionResult> ChatRequests()
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var volunteerId = GetVolunteerId()!.Value;

            var activeRequests = await _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.AssignedVolunteerId == volunteerId && (r.Status == "Accepted" || r.Status == "InProgress"))
                .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                .ToListAsync();

            return View(activeRequests);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");

            var volunteerId = GetVolunteerId()!.Value;
            var volunteer = await _context.Volunteers.FindAsync(volunteerId);

            if (volunteer != null)
            {
                volunteer.IsAvailable = !volunteer.IsAvailable;
                volunteer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = volunteer.IsAvailable ? "You are now marked as Available for nearby support requests." : "You are now marked as Unavailable.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult CommunityActivities()
        {
            if (!IsVolunteer()) return RedirectToAction("Login", "Account");
            SetUserInfo();
            return View();
        }
    }
}