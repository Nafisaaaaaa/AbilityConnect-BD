using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.IO;
using System.Linq;

namespace SDP1.Controllers
{
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public OrganizationController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private bool IsOrganization()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Organization";
        }

        private int GetCurrentOrgId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private void SetUserInfo()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Organization";
            ViewBag.Logo = HttpContext.Session.GetString("Logo") ?? "/images/default-avatar.png";
        }

        public IActionResult Dashboard()
        {
            if (!IsOrganization())
                return RedirectToAction("Login", "Account");

            SetUserInfo();

            var orgId = GetCurrentOrgId();
            ViewBag.JobPostCount = _context.Jobs.Count(j => j.OrganizationId == orgId);
            ViewBag.TrainingCount = _context.TrainingPrograms.Count(t => t.OrganizationId == orgId);
            ViewBag.ScholarshipCount = _context.Scholarships.Count(s => s.OrganizationId == orgId);
            ViewBag.EventCount = _context.AwarenessEvents.Count(e => e.OrganizationId == orgId);

            return View();
        }

        public IActionResult ManageJobPosts()
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            return RedirectToAction("Manage", "Jobs");
        }

        public IActionResult ReviewApplications()
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            return RedirectToAction("ReviewApplications", "Jobs");
        }

        public IActionResult UserQueries()
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();
            return View();
        }

        

        public IActionResult TrainingPrograms(string? search, string? category)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var query = _context.TrainingPrograms
                .Include(t => t.Registrations)
                .Where(t => t.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(s) || t.SkillsCovered.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.TrainingCategory == category);
            }

            var trainings = query.OrderByDescending(t => t.CreatedAt).ToList();
            ViewBag.Search = search;
            ViewBag.Category = category;

            return View(trainings);
        }

        [HttpGet]
        public IActionResult CreateTraining()
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var model = new TrainingProgram
            {
                StartDate = DateTime.UtcNow.Date.AddDays(7),
                EndDate = DateTime.UtcNow.Date.AddMonths(3),
                RegistrationDeadline = DateTime.UtcNow.Date.AddDays(5),
                DeliveryMode = "Online",
                TrainingCategory = "Computer Training",
                Duration = "3 Months",
                Eligibility = "Open to persons with disabilities"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTraining(TrainingProgram model)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }
            if (model.RegistrationDeadline > model.StartDate)
            {
                ModelState.AddModelError("RegistrationDeadline", "Registration deadline should be on or before start date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            model.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            model.RegistrationDeadline = DateTime.SpecifyKind(model.RegistrationDeadline, DateTimeKind.Utc);
            model.OrganizationId = orgId;
            model.CreatedAt = DateTime.UtcNow;
            model.Status = "Active";

            _context.TrainingPrograms.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Training program '{model.Title}' created successfully.";
            return RedirectToAction(nameof(TrainingPrograms));
        }

        [HttpGet]
        public IActionResult EditTraining(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var training = _context.TrainingPrograms.FirstOrDefault(t => t.Id == id && t.OrganizationId == orgId);
            if (training == null) return NotFound();

            return View(training);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTraining(int id, TrainingProgram model)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var training = _context.TrainingPrograms.FirstOrDefault(t => t.Id == id && t.OrganizationId == orgId);
            if (training == null) return NotFound();

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            training.Title = model.Title;
            training.Description = model.Description;
            training.TrainingCategory = model.TrainingCategory;
            training.SkillsCovered = model.SkillsCovered;
            training.Duration = model.Duration;
            training.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            training.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            training.RegistrationDeadline = DateTime.SpecifyKind(model.RegistrationDeadline, DateTimeKind.Utc);
            training.Location = model.Location;
            training.DeliveryMode = model.DeliveryMode;
            training.Eligibility = model.Eligibility;
            training.MaxParticipants = model.MaxParticipants;
            training.ContactInfo = model.ContactInfo;
            training.Status = model.Status;
            training.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Training program '{training.Title}' updated successfully.";
            return RedirectToAction(nameof(TrainingPrograms));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTraining(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var training = _context.TrainingPrograms.FirstOrDefault(t => t.Id == id && t.OrganizationId == orgId);
            if (training == null) return NotFound();

            _context.TrainingPrograms.Remove(training);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Training program '{training.Title}' deleted successfully.";
            return RedirectToAction(nameof(TrainingPrograms));
        }

        public IActionResult TrainingRegistrations(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var training = _context.TrainingPrograms
                .Include(t => t.Registrations)
                    .ThenInclude(r => r.DisabilityUser)
                .FirstOrDefault(t => t.Id == id && t.OrganizationId == orgId);

            if (training == null) return NotFound();

            return View(training);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateTrainingRegistrationStatus(int id, string status)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var registration = _context.TrainingRegistrations
                .Include(r => r.TrainingProgram)
                .FirstOrDefault(r => r.Id == id && r.TrainingProgram.OrganizationId == orgId);

            if (registration == null) return NotFound();

            registration.Status = status;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Participant registration status updated.";
            return RedirectToAction(nameof(TrainingRegistrations), new { id = registration.TrainingProgramId });
        }

        

        public IActionResult ManageScholarships(string? search, string? type)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var query = _context.Scholarships
                .Include(s => s.Applications)
                .Where(s => s.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(sc => sc.Title.ToLower().Contains(s) || sc.FieldOfStudy.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(sc => sc.ScholarshipType == type);
            }

            var scholarships = query.OrderByDescending(s => s.CreatedAt).ToList();
            ViewBag.Search = search;
            ViewBag.Type = type;

            return View(scholarships);
        }

        [HttpGet]
        public IActionResult CreateScholarship()
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var model = new Scholarship
            {
                ApplicationDeadline = DateTime.UtcNow.Date.AddDays(14),
                ScholarshipType = "Full Funding",
                FieldOfStudy = "General / Any",
                Location = "Online / Bangladesh",
                Eligibility = "Persons with disabilities pursuing education or vocational training."
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateScholarship(Scholarship model)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.ApplicationDeadline = DateTime.SpecifyKind(model.ApplicationDeadline, DateTimeKind.Utc);
            if (model.StartDate.HasValue)
            {
                model.StartDate = DateTime.SpecifyKind(model.StartDate.Value, DateTimeKind.Utc);
            }
            model.OrganizationId = orgId;
            model.CreatedAt = DateTime.UtcNow;
            model.Status = "Active";

            _context.Scholarships.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Scholarship '{model.Title}' posted successfully.";
            return RedirectToAction(nameof(ManageScholarships));
        }

        [HttpGet]
        public IActionResult EditScholarship(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var scholarship = _context.Scholarships.FirstOrDefault(s => s.Id == id && s.OrganizationId == orgId);
            if (scholarship == null) return NotFound();

            return View(scholarship);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditScholarship(int id, Scholarship model)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var scholarship = _context.Scholarships.FirstOrDefault(s => s.Id == id && s.OrganizationId == orgId);
            if (scholarship == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            scholarship.Title = model.Title;
            scholarship.Description = model.Description;
            scholarship.ScholarshipType = model.ScholarshipType;
            scholarship.FieldOfStudy = model.FieldOfStudy;
            scholarship.Benefits = model.Benefits;
            scholarship.Eligibility = model.Eligibility;
            scholarship.RequiredQualifications = model.RequiredQualifications;
            scholarship.ApplicationDeadline = DateTime.SpecifyKind(model.ApplicationDeadline, DateTimeKind.Utc);
            if (model.StartDate.HasValue)
            {
                scholarship.StartDate = DateTime.SpecifyKind(model.StartDate.Value, DateTimeKind.Utc);
            }
            else
            {
                scholarship.StartDate = null;
            }
            scholarship.Location = model.Location;
            scholarship.ContactInfo = model.ContactInfo;
            scholarship.Status = model.Status;
            scholarship.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Scholarship '{scholarship.Title}' updated successfully.";
            return RedirectToAction(nameof(ManageScholarships));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteScholarship(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var scholarship = _context.Scholarships.FirstOrDefault(s => s.Id == id && s.OrganizationId == orgId);
            if (scholarship == null) return NotFound();

            _context.Scholarships.Remove(scholarship);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Scholarship '{scholarship.Title}' deleted successfully.";
            return RedirectToAction(nameof(ManageScholarships));
        }

        public IActionResult ScholarshipApplications(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var scholarship = _context.Scholarships
                .Include(s => s.Applications)
                    .ThenInclude(a => a.DisabilityUser)
                .FirstOrDefault(s => s.Id == id && s.OrganizationId == orgId);

            if (scholarship == null) return NotFound();

            return View(scholarship);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateScholarshipApplicationStatus(int id, string status)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var application = _context.ScholarshipApplications
                .Include(a => a.Scholarship)
                .FirstOrDefault(a => a.Id == id && a.Scholarship.OrganizationId == orgId);

            if (application == null) return NotFound();

            application.Status = status;
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Application status updated to '{status}'.";
            return RedirectToAction(nameof(ScholarshipApplications), new { id = application.ScholarshipId });
        }

        [HttpGet]
        public IActionResult DownloadCV(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var application = _context.ScholarshipApplications
                .Include(a => a.Scholarship)
                .FirstOrDefault(a => a.Id == id && a.Scholarship.OrganizationId == orgId);

            if (application == null || string.IsNullOrEmpty(application.CVFilePath))
                return NotFound("Application or CV file not found.");

            var relativePath = application.CVFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("The requested CV file does not exist on the server.");

            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            else if (ext == ".doc") contentType = "application/msword";

            return PhysicalFile(fullPath, contentType, application.CVOriginalFileName);
        }

        

        public IActionResult ManageEvents(string? search, string? eventType)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var query = _context.AwarenessEvents
                .Include(e => e.Registrations)
                .Where(e => e.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(ev => ev.Title.ToLower().Contains(s) || ev.Location.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(ev => ev.EventType == eventType);
            }

            var events = query.OrderByDescending(e => e.CreatedAt).ToList();
            ViewBag.Search = search;
            ViewBag.EventType = eventType;

            return View(events);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var model = new AwarenessEvent
            {
                EventDate = DateTime.UtcNow.Date.AddDays(7),
                RegistrationDeadline = DateTime.UtcNow.Date.AddDays(5),
                StartTime = "10:00 AM",
                EndTime = "01:00 PM",
                EventType = "Disability Awareness",
                Location = "Online via Zoom",
                Eligibility = "Open to everyone interested in disability empowerment"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEvent(AwarenessEvent model)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.EventDate = DateTime.SpecifyKind(model.EventDate, DateTimeKind.Utc);
            model.RegistrationDeadline = DateTime.SpecifyKind(model.RegistrationDeadline, DateTimeKind.Utc);
            model.OrganizationId = orgId;
            model.CreatedAt = DateTime.UtcNow;
            model.Status = "Active";

            _context.AwarenessEvents.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Event '{model.Title}' created successfully.";
            return RedirectToAction(nameof(ManageEvents));
        }

        [HttpGet]
        public IActionResult EditEvent(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var ev = _context.AwarenessEvents.FirstOrDefault(e => e.Id == id && e.OrganizationId == orgId);
            if (ev == null) return NotFound();

            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEvent(int id, AwarenessEvent model)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var ev = _context.AwarenessEvents.FirstOrDefault(e => e.Id == id && e.OrganizationId == orgId);
            if (ev == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.EventDate = DateTime.SpecifyKind(model.EventDate, DateTimeKind.Utc);
            ev.StartTime = model.StartTime;
            ev.EndTime = model.EndTime;
            ev.Location = model.Location;
            ev.EventType = model.EventType;
            ev.Eligibility = model.Eligibility;
            ev.RegistrationDeadline = DateTime.SpecifyKind(model.RegistrationDeadline, DateTimeKind.Utc);
            ev.ContactInfo = model.ContactInfo;
            ev.Status = model.Status;
            ev.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Event '{ev.Title}' updated successfully.";
            return RedirectToAction(nameof(ManageEvents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEvent(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var ev = _context.AwarenessEvents.FirstOrDefault(e => e.Id == id && e.OrganizationId == orgId);
            if (ev == null) return NotFound();

            _context.AwarenessEvents.Remove(ev);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Event '{ev.Title}' deleted successfully.";
            return RedirectToAction(nameof(ManageEvents));
        }

        public IActionResult EventRegistrations(int id)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");
            SetUserInfo();

            var orgId = GetCurrentOrgId();
            var ev = _context.AwarenessEvents
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.DisabilityUser)
                .FirstOrDefault(e => e.Id == id && e.OrganizationId == orgId);

            if (ev == null) return NotFound();

            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateEventRegistrationStatus(int id, string status)
        {
            if (!IsOrganization()) return RedirectToAction("Login", "Account");

            var orgId = GetCurrentOrgId();
            var registration = _context.EventRegistrations
                .Include(r => r.AwarenessEvent)
                .FirstOrDefault(r => r.Id == id && r.AwarenessEvent.OrganizationId == orgId);

            if (registration == null) return NotFound();

            registration.Status = status;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Participant registration status updated.";
            return RedirectToAction(nameof(EventRegistrations), new { id = registration.AwarenessEventId });
        }
    }
}