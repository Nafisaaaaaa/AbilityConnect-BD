using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SDP1.Controllers
{
    public class VolunteerSupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerSupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Session / Role Helpers

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetUserRole() => HttpContext.Session.GetString("UserRole");
        private string? GetUserName() => HttpContext.Session.GetString("UserName");
        private bool IsDisabilityUser() => GetUserRole() == "Disability";
        private bool IsVolunteer() => GetUserRole() == "Volunteer";
        private bool IsAdmin() => GetUserRole() == "Admin";

        #endregion

        
        public async Task<IActionResult> Index(string? statusFilter)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Person with Disability to view your volunteer requests.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var query = _context.VolunteerSupportRequests
                .Include(r => r.AssignedVolunteer)
                .Where(r => r.RequestedByUserId == userId);

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var allUserRequests = await _context.VolunteerSupportRequests
                .Where(r => r.RequestedByUserId == userId)
                .ToListAsync();

            var viewModel = new VolunteerSupportRequestListViewModel
            {
                Requests = requests,
                StatusFilter = statusFilter ?? "All",
                PendingCount = allUserRequests.Count(r => r.Status == "Pending"),
                ActiveCount = allUserRequests.Count(r => r.Status == "Accepted" || r.Status == "InProgress"),
                CompletedCount = allUserRequests.Count(r => r.Status == "Completed")
            };

            return View(viewModel);
        }

       
        [HttpGet]
        public async Task<IActionResult> Create(string? assistanceType, string? city, string? district)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Person with Disability to request volunteer support.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var user = await _context.DisabilityUsers.FindAsync(userId);

            var defaultCity = city ?? (user != null && !string.IsNullOrEmpty(user.Location) ? user.Location.Split(',')[0].Trim() : "Dhaka");
            var defaultDistrict = district ?? defaultCity;

            var matchingVolunteersQuery = _context.Volunteers
                .Where(v => v.IsVerified && v.IsAvailable);

            if (!string.IsNullOrWhiteSpace(defaultCity))
            {
                var cTerm = defaultCity.Trim().ToLower();
                matchingVolunteersQuery = matchingVolunteersQuery.Where(v =>
                    (v.City != null && v.City.ToLower().Contains(cTerm)) ||
                    (v.Location != null && v.Location.ToLower().Contains(cTerm)));
            }

            var matchingVolunteers = await matchingVolunteersQuery.Take(6).ToListAsync();

            var model = new VolunteerSupportRequestCreateViewModel
            {
                AssistanceType = assistanceType ?? "Hospital Visits",
                City = defaultCity,
                District = defaultDistrict,
                RequestDate = DateTime.Today.AddDays(1),
                RequestTime = "10:00 AM",
                MatchingVolunteers = matchingVolunteers
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VolunteerSupportRequestCreateViewModel model)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;

            var validAssistanceTypes = new[]
            {
                "Hospital Visits",
                "Shopping Assistance",
                "Travel Assistance",
                "Document Submission",
                "Daily Support"
            };

            if (!validAssistanceTypes.Contains(model.AssistanceType))
            {
                ModelState.AddModelError("AssistanceType", "Please select a valid assistance type.");
            }

            if (model.RequestDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("RequestDate", "Request date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
               
                model.MatchingVolunteers = await _context.Volunteers
                    .Where(v => v.IsVerified && v.IsAvailable)
                    .Take(6)
                    .ToListAsync();
                return View(model);
            }

            var request = new VolunteerSupportRequest
            {
                RequestedByUserId = userId,
                AssistanceType = model.AssistanceType,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                RequestDate = DateTime.SpecifyKind(model.RequestDate, DateTimeKind.Utc),
                RequestTime = model.RequestTime.Trim(),
                Location = model.Location.Trim(),
                City = model.City.Trim(),
                District = model.District.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Urgency = model.Urgency,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.VolunteerSupportRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your volunteer support request has been broadcasted! Matching nearby volunteers have been notified.";
            return RedirectToAction("Details", new { id = request.Id });
        }
      
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.AssignedVolunteer)
                .Include(r => r.ChatMessages)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            var userId = GetUserId();
            var role = GetUserRole();


            bool isRequester = role == "Disability" && request.RequestedByUserId == userId;
            bool isAssigned = role == "Volunteer" && request.AssignedVolunteerId == userId;
            bool isVolunteerBrowsing = role == "Volunteer" && request.Status == "Pending";
            bool isAdmin = role == "Admin";

            if (!isRequester && !isAssigned && !isVolunteerBrowsing && !isAdmin)
            {
                TempData["ErrorMessage"] = "You do not have access to view this support request.";
                return RedirectToAction("Index", "Home");
            }

            
            List<Volunteer> nearbyMatches = new();
            if (request.Status == "Pending")
            {
                nearbyMatches = await _context.Volunteers
                    .Where(v => v.IsVerified && v.IsAvailable &&
                        ((v.City != null && v.City.ToLower().Contains(request.City.ToLower())) ||
                         (v.Location != null && v.Location.ToLower().Contains(request.City.ToLower()))))
                    .Take(4)
                    .ToListAsync();
            }
            ViewBag.NearbyMatches = nearbyMatches;

            return View(request);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancellationReason)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var request = await _context.VolunteerSupportRequests.FindAsync(id);

            if (request == null) return NotFound();

            if (request.RequestedByUserId != userId)
            {
                TempData["ErrorMessage"] = "You do not have permission to cancel this request.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status == "Completed" || request.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "This request is already closed and cannot be cancelled.";
                return RedirectToAction("Details", new { id });
            }

            request.Status = "Cancelled";
            request.CancellationReason = cancellationReason?.Trim() ?? "Cancelled by user";
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Support request has been cancelled.";
            return RedirectToAction(nameof(Index));
        }

        
        public async Task<IActionResult> Chat(int id)
        {
            var request = await _context.VolunteerSupportRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.AssignedVolunteer)
                .Include(r => r.ChatMessages)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            var userId = GetUserId();
            var role = GetUserRole();
            var userName = GetUserName() ?? "User";

            if (!userId.HasValue || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "Please log in to access the chat.";
                return RedirectToAction("Login", "Account");
            }

            bool isRequester = role == "Disability" && request.RequestedByUserId == userId.Value;
            bool isAssigned = role == "Volunteer" && request.AssignedVolunteerId == userId.Value;
            bool isAdmin = role == "Admin";

            if (!isRequester && !isAssigned && !isAdmin)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this chat.";
                return RedirectToAction("Index", "Home");
            }

            var messages = await _context.VolunteerChatMessages
                .Where(m => m.VolunteerSupportRequestId == id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            string otherPartyName = isRequester ? (request.AssignedVolunteer?.FullName ?? "Volunteer") : (request.RequestedByUser?.FullName ?? "Disability User");
            string otherPartyRole = isRequester ? "Volunteer" : "Disability User";
            string? otherPartyPhone = isRequester ? request.AssignedVolunteer?.PhoneNumber : request.RequestedByUser?.PhoneNumber;

            var viewModel = new VolunteerChatViewModel
            {
                Request = request,
                Messages = messages,
                CurrentUserId = userId.Value,
                CurrentUserRole = role,
                CurrentUserName = userName,
                OtherPartyName = otherPartyName,
                OtherPartyRole = otherPartyRole,
                OtherPartyPhone = otherPartyPhone
            };

            return View(viewModel);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int requestId, string messageText)
        {
            var userId = GetUserId();
            var role = GetUserRole();
            var userName = GetUserName() ?? "User";

            if (!userId.HasValue || string.IsNullOrEmpty(role) || string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest();
            }

            var request = await _context.VolunteerSupportRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            bool isRequester = role == "Disability" && request.RequestedByUserId == userId.Value;
            bool isAssigned = role == "Volunteer" && request.AssignedVolunteerId == userId.Value;
            bool isAdmin = role == "Admin";

            if (!isRequester && !isAssigned && !isAdmin)
            {
                return Forbid();
            }

            var msg = new VolunteerChatMessage
            {
                VolunteerSupportRequestId = requestId,
                SenderUserId = userId.Value,
                SenderRole = role,
                SenderName = userName,
                MessageText = messageText.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.VolunteerChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    id = msg.Id,
                    senderUserId = msg.SenderUserId,
                    senderRole = msg.SenderRole,
                    senderName = msg.SenderName,
                    messageText = msg.MessageText,
                    sentAt = msg.SentAt.ToString("hh:mm tt")
                });
            }

            return RedirectToAction("Chat", new { id = requestId });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int requestId)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            if (!userId.HasValue || string.IsNullOrEmpty(role))
            {
                return Unauthorized();
            }

            var request = await _context.VolunteerSupportRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            bool isRequester = role == "Disability" && request.RequestedByUserId == userId.Value;
            bool isAssigned = role == "Volunteer" && request.AssignedVolunteerId == userId.Value;
            bool isAdmin = role == "Admin";

            if (!isRequester && !isAssigned && !isAdmin)
            {
                return Forbid();
            }

            var messages = await _context.VolunteerChatMessages
                .Where(m => m.VolunteerSupportRequestId == requestId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    id = m.Id,
                    senderUserId = m.SenderUserId,
                    senderRole = m.SenderRole,
                    senderName = m.SenderName,
                    messageText = m.MessageText,
                    sentAt = m.SentAt.ToString("hh:mm tt")
                })
                .ToListAsync();

            return Json(messages);
        }
    }
}
