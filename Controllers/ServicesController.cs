using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SDP1.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ServicesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private bool IsDisabilityUser()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Disability";
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private void SetUserInfo()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
        }

       

        public IActionResult Trainings(string? search, string? category, string? deliveryMode, string? sort, int page = 1)
        {
            SetUserInfo();
            var pageSize = 9;
            var userId = GetCurrentUserId();
            var isDisability = IsDisabilityUser();

            var query = _context.TrainingPrograms
                .Include(t => t.Organization)
                .Include(t => t.Registrations)
                .Where(t => t.Status == "Active");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(s) || 
                                         t.SkillsCovered.ToLower().Contains(s) || 
                                         t.Location.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.TrainingCategory == category);
            }

            if (!string.IsNullOrWhiteSpace(deliveryMode))
            {
                query = query.Where(t => t.DeliveryMode == deliveryMode);
            }

            query = sort switch
            {
                "deadline" => query.OrderBy(t => t.RegistrationDeadline),
                "startDate" => query.OrderBy(t => t.StartDate),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var trainings = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var registeredIds = new HashSet<int>();
            var savedIds = new HashSet<int>();

            if (isDisability && userId > 0)
            {
                registeredIds = _context.TrainingRegistrations
                    .Where(r => r.DisabilityUserId == userId)
                    .Select(r => r.TrainingProgramId)
                    .ToHashSet();

                savedIds = _context.SavedOpportunities
                    .Where(s => s.DisabilityUserId == userId && s.OpportunityType == OpportunityTypes.Training)
                    .Select(s => s.OpportunityId)
                    .ToHashSet();
            }

            var categories = _context.TrainingPrograms
                .Select(t => t.TrainingCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var viewModel = new TrainingListViewModel
            {
                Trainings = trainings,
                SearchKeyword = search,
                Category = category,
                DeliveryMode = deliveryMode,
                SortOrder = sort,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                AvailableCategories = categories,
                RegisteredTrainingIds = registeredIds,
                SavedTrainingIds = savedIds
            };

            return View(viewModel);
        }

        public IActionResult TrainingDetails(int id)
        {
            SetUserInfo();
            var training = _context.TrainingPrograms
                .Include(t => t.Organization)
                .Include(t => t.Registrations)
                .FirstOrDefault(t => t.Id == id);

            if (training == null) return NotFound();

            var userId = GetCurrentUserId();
            var isDisability = IsDisabilityUser();
            var userRole = HttpContext.Session.GetString("UserRole");

            var viewModel = new TrainingDetailsViewModel
            {
                Training = training,
                RegistrationCount = training.Registrations.Count,
                IsRegistered = isDisability && userId > 0 && training.Registrations.Any(r => r.DisabilityUserId == userId),
                IsSaved = isDisability && userId > 0 && _context.SavedOpportunities.Any(s => s.DisabilityUserId == userId && s.OpportunityType == OpportunityTypes.Training && s.OpportunityId == id),
                IsOrganizationOwner = userRole == "Organization" && training.OrganizationId == userId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterTraining(int id)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in as a registered Disability user to enroll in training programs.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("TrainingDetails", new { id }) });
            }

            var userId = GetCurrentUserId();
            var training = _context.TrainingPrograms
                .Include(t => t.Registrations)
                .FirstOrDefault(t => t.Id == id);

            if (training == null) return NotFound();

            if (training.Status != "Active")
            {
                TempData["ErrorMessage"] = "This training program is no longer active.";
                return RedirectToAction(nameof(TrainingDetails), new { id });
            }

            if (DateTime.UtcNow.Date > training.RegistrationDeadline.Date)
            {
                TempData["ErrorMessage"] = "The registration deadline for this training has passed.";
                return RedirectToAction(nameof(TrainingDetails), new { id });
            }

            if (training.MaxParticipants.HasValue && training.Registrations.Count >= training.MaxParticipants.Value)
            {
                TempData["ErrorMessage"] = "Sorry, this training program has reached maximum capacity.";
                return RedirectToAction(nameof(TrainingDetails), new { id });
            }

            var alreadyRegistered = _context.TrainingRegistrations
                .Any(r => r.TrainingProgramId == id && r.DisabilityUserId == userId);

            if (alreadyRegistered)
            {
                TempData["InfoMessage"] = "You are already enrolled in this training program.";
                return RedirectToAction(nameof(TrainingDetails), new { id });
            }

            var registration = new TrainingRegistration
            {
                TrainingProgramId = id,
                DisabilityUserId = userId,
                RegisteredAt = DateTime.UtcNow,
                Status = "Registered"
            };

            _context.TrainingRegistrations.Add(registration);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Congratulations! You have successfully enrolled in '{training.Title}'.";
            return RedirectToAction(nameof(TrainingDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelTrainingRegistration(int id)
        {
            if (!IsDisabilityUser()) return RedirectToAction("Login", "Account");

            var userId = GetCurrentUserId();
            var registration = _context.TrainingRegistrations
                .FirstOrDefault(r => r.TrainingProgramId == id && r.DisabilityUserId == userId);

            if (registration != null)
            {
                _context.TrainingRegistrations.Remove(registration);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Your enrollment has been cancelled.";
            }

            return RedirectToAction(nameof(TrainingDetails), new { id });
        }

      

        public IActionResult Scholarships(string? search, string? type, string? sort, int page = 1)
        {
            SetUserInfo();
            var pageSize = 9;
            var userId = GetCurrentUserId();
            var isDisability = IsDisabilityUser();

            var query = _context.Scholarships
                .Include(s => s.Organization)
                .Include(s => s.Applications)
                .Where(s => s.Status == "Active");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(sc => sc.Title.ToLower().Contains(s) || 
                                          sc.FieldOfStudy.ToLower().Contains(s) || 
                                          sc.Benefits.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(sc => sc.ScholarshipType == type);
            }

            query = sort switch
            {
                "deadline" => query.OrderBy(sc => sc.ApplicationDeadline),
                _ => query.OrderByDescending(sc => sc.CreatedAt)
            };

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var scholarships = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var appliedIds = new HashSet<int>();
            var savedIds = new HashSet<int>();

            if (isDisability && userId > 0)
            {
                appliedIds = _context.ScholarshipApplications
                    .Where(a => a.DisabilityUserId == userId)
                    .Select(a => a.ScholarshipId)
                    .ToHashSet();

                savedIds = _context.SavedOpportunities
                    .Where(s => s.DisabilityUserId == userId && s.OpportunityType == OpportunityTypes.Scholarship)
                    .Select(s => s.OpportunityId)
                    .ToHashSet();
            }

            var types = _context.Scholarships
                .Select(s => s.ScholarshipType)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var viewModel = new ScholarshipListViewModel
            {
                Scholarships = scholarships,
                SearchKeyword = search,
                ScholarshipType = type,
                SortOrder = sort,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                AvailableTypes = types,
                AppliedScholarshipIds = appliedIds,
                SavedScholarshipIds = savedIds
            };

            return View(viewModel);
        }

        public IActionResult ScholarshipDetails(int id)
        {
            SetUserInfo();
            var scholarship = _context.Scholarships
                .Include(s => s.Organization)
                .Include(s => s.Applications)
                .FirstOrDefault(s => s.Id == id);

            if (scholarship == null) return NotFound();

            var userId = GetCurrentUserId();
            var isDisability = IsDisabilityUser();
            var userRole = HttpContext.Session.GetString("UserRole");

            var application = isDisability && userId > 0 
                ? scholarship.Applications.FirstOrDefault(a => a.DisabilityUserId == userId)
                : null;

            var viewModel = new ScholarshipDetailsViewModel
            {
                Scholarship = scholarship,
                ApplicationCount = scholarship.Applications.Count,
                HasApplied = application != null,
                ApplicationStatus = application?.Status,
                IsSaved = isDisability && userId > 0 && _context.SavedOpportunities.Any(s => s.DisabilityUserId == userId && s.OpportunityType == OpportunityTypes.Scholarship && s.OpportunityId == id),
                IsOrganizationOwner = userRole == "Organization" && scholarship.OrganizationId == userId
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult ApplyScholarship(int id)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in as a registered Disability user to apply for scholarships.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ApplyScholarship", new { id }) });
            }

            var userId = GetCurrentUserId();
            var scholarship = _context.Scholarships
                .Include(s => s.Organization)
                .FirstOrDefault(s => s.Id == id);

            if (scholarship == null) return NotFound();

            if (scholarship.Status != "Active" || DateTime.UtcNow.Date > scholarship.ApplicationDeadline.Date)
            {
                TempData["ErrorMessage"] = "Applications for this scholarship are currently closed.";
                return RedirectToAction(nameof(ScholarshipDetails), new { id });
            }

            var existingApp = _context.ScholarshipApplications
                .FirstOrDefault(a => a.ScholarshipId == id && a.DisabilityUserId == userId);

            if (existingApp != null)
            {
                TempData["InfoMessage"] = $"You have already applied for this scholarship. Current status: {existingApp.Status}.";
                return RedirectToAction(nameof(ScholarshipDetails), new { id });
            }

            var user = _context.DisabilityUsers.Find(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new ScholarshipApplicationInputModel
            {
                ScholarshipId = id,
                ScholarshipTitle = scholarship.Title,
                OrganizationName = scholarship.Organization?.OrganizationName ?? "Organization",
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Address = user.Location,
                HighestQualification = user.Education ?? string.Empty,
                TechnicalSkills = user.Skills ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyScholarship(ScholarshipApplicationInputModel model)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in as a registered Disability user.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            var scholarship = _context.Scholarships
                .Include(s => s.Organization)
                .FirstOrDefault(s => s.Id == model.ScholarshipId);

            if (scholarship == null) return NotFound();

            model.ScholarshipTitle = scholarship.Title;
            model.OrganizationName = scholarship.Organization?.OrganizationName;

            
            var existingApp = _context.ScholarshipApplications
                .FirstOrDefault(a => a.ScholarshipId == model.ScholarshipId && a.DisabilityUserId == userId);

            if (existingApp != null)
            {
                TempData["InfoMessage"] = "You have already submitted an application for this scholarship.";
                return RedirectToAction(nameof(ScholarshipDetails), new { id = model.ScholarshipId });
            }

           
            if (model.CVFile == null || model.CVFile.Length == 0)
            {
                ModelState.AddModelError("CVFile", "Please upload your CV/Resume.");
            }
            else
            {
                var allowedExts = new[] { ".pdf", ".doc", ".docx" };
                var ext = Path.GetExtension(model.CVFile.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(ext))
                {
                    ModelState.AddModelError("CVFile", "Only PDF, DOC, or DOCX files are allowed.");
                }
                else if (model.CVFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("CVFile", "CV file size cannot exceed 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var extFinal = Path.GetExtension(model.CVFile!.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{extFinal}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "scholarship_cvs");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                model.CVFile.CopyTo(stream);
            }

            var application = new ScholarshipApplication
            {
                ScholarshipId = model.ScholarshipId,
                DisabilityUserId = userId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                Address = model.Address.Trim(),
                HighestQualification = model.HighestQualification.Trim(),
                Institution = model.Institution.Trim(),
                FieldOfStudy = model.FieldOfStudy.Trim(),
                PassingYear = model.PassingYear.Trim(),
                AcademicResult = model.AcademicResult?.Trim(),
                TechnicalSkills = model.TechnicalSkills.Trim(),
                ProfessionalSkills = model.ProfessionalSkills?.Trim(),
                OtherSkills = model.OtherSkills?.Trim(),
                Motivation = model.Motivation.Trim(),
                RelevantExperience = model.RelevantExperience?.Trim(),
                CVFilePath = $"/uploads/scholarship_cvs/{uniqueFileName}",
                CVOriginalFileName = Path.GetFileName(model.CVFile.FileName),
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.ScholarshipApplications.Add(application);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Your application for '{scholarship.Title}' has been submitted successfully!";
            return RedirectToAction(nameof(ScholarshipDetails), new { id = model.ScholarshipId });
        }

        public IActionResult Events(string? search, string? eventType, string? sort, int page = 1)
        {
            SetUserInfo();
            var pageSize = 9;
            var userId = GetCurrentUserId();
            var isDisability = IsDisabilityUser();

            var query = _context.AwarenessEvents
                .Include(e => e.Organization)
                .Include(e => e.Registrations)
                .Where(e => e.Status == "Active");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(s) || 
                                         e.Location.ToLower().Contains(s) || 
                                         e.Description.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            query = sort switch
            {
                "deadline" => query.OrderBy(e => e.RegistrationDeadline),
                "eventDate" => query.OrderBy(e => e.EventDate),
                _ => query.OrderByDescending(e => e.CreatedAt)
            };

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var events = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var registeredIds = new HashSet<int>();
            var savedIds = new HashSet<int>();

            if (isDisability && userId > 0)
            {
                registeredIds = _context.EventRegistrations
                    .Where(r => r.DisabilityUserId == userId)
                    .Select(r => r.AwarenessEventId)
                    .ToHashSet();

                savedIds = _context.SavedOpportunities
                    .Where(s => s.DisabilityUserId == userId && s.OpportunityType == OpportunityTypes.Event)
                    .Select(s => s.OpportunityId)
                    .ToHashSet();
            }

            var eventTypes = _context.AwarenessEvents
                .Select(e => e.EventType)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var viewModel = new EventListViewModel
            {
                Events = events,
                SearchKeyword = search,
                EventType = eventType,
                SortOrder = sort,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                AvailableEventTypes = eventTypes,
                RegisteredEventIds = registeredIds,
                SavedEventIds = savedIds
            };

            return View(viewModel);
        }

        public IActionResult EventDetails(int id)
        {
            SetUserInfo();
            var ev = _context.AwarenessEvents
                .Include(e => e.Organization)
                .Include(e => e.Registrations)
                .FirstOrDefault(e => e.Id == id);

            if (ev == null) return NotFound();

            var userId = GetCurrentUserId();
            var isDisability = IsDisabilityUser();
            var userRole = HttpContext.Session.GetString("UserRole");

            var viewModel = new EventDetailsViewModel
            {
                Event = ev,
                RegistrationCount = ev.Registrations.Count,
                IsRegistered = isDisability && userId > 0 && ev.Registrations.Any(r => r.DisabilityUserId == userId),
                IsSaved = isDisability && userId > 0 && _context.SavedOpportunities.Any(s => s.DisabilityUserId == userId && s.OpportunityType == OpportunityTypes.Event && s.OpportunityId == id),
                IsOrganizationOwner = userRole == "Organization" && ev.OrganizationId == userId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterEvent(int id)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in as a registered Disability user to register for events.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("EventDetails", new { id }) });
            }

            var userId = GetCurrentUserId();
            var ev = _context.AwarenessEvents
                .Include(e => e.Registrations)
                .FirstOrDefault(e => e.Id == id);

            if (ev == null) return NotFound();

            if (ev.Status != "Active")
            {
                TempData["ErrorMessage"] = "This event is not accepting registrations.";
                return RedirectToAction(nameof(EventDetails), new { id });
            }

            if (DateTime.UtcNow.Date > ev.RegistrationDeadline.Date)
            {
                TempData["ErrorMessage"] = "The registration deadline for this event has passed.";
                return RedirectToAction(nameof(EventDetails), new { id });
            }

            var alreadyRegistered = _context.EventRegistrations
                .Any(r => r.AwarenessEventId == id && r.DisabilityUserId == userId);

            if (alreadyRegistered)
            {
                TempData["InfoMessage"] = "You are already registered for this event.";
                return RedirectToAction(nameof(EventDetails), new { id });
            }

            var registration = new EventRegistration
            {
                AwarenessEventId = id,
                DisabilityUserId = userId,
                RegisteredAt = DateTime.UtcNow,
                Status = "Registered"
            };

            _context.EventRegistrations.Add(registration);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"You have registered successfully for '{ev.Title}'.";
            return RedirectToAction(nameof(EventDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelEventRegistration(int id)
        {
            if (!IsDisabilityUser()) return RedirectToAction("Login", "Account");

            var userId = GetCurrentUserId();
            var registration = _context.EventRegistrations
                .FirstOrDefault(r => r.AwarenessEventId == id && r.DisabilityUserId == userId);

            if (registration != null)
            {
                _context.EventRegistrations.Remove(registration);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Your event registration has been cancelled.";
            }

            return RedirectToAction(nameof(EventDetails), new { id });
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleSave(string type, int id, string? returnUrl)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in as a Disability user to bookmark opportunities.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            var existing = _context.SavedOpportunities
                .FirstOrDefault(s => s.DisabilityUserId == userId && s.OpportunityType == type && s.OpportunityId == id);

            if (existing != null)
            {
                _context.SavedOpportunities.Remove(existing);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Removed from your saved opportunities.";
            }
            else
            {
                var saved = new SavedOpportunity
                {
                    DisabilityUserId = userId,
                    OpportunityType = type,
                    OpportunityId = id,
                    SavedAt = DateTime.UtcNow
                };
                _context.SavedOpportunities.Add(saved);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Saved to your bookmarks!";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(SavedOpportunities));
        }

        [HttpGet]
        public IActionResult SavedOpportunities(string? type = "All")
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in to view your saved opportunities.";
                return RedirectToAction("Login", "Account");
            }

            SetUserInfo();
            var userId = GetCurrentUserId();

            var savedItems = _context.SavedOpportunities
                .Where(s => s.DisabilityUserId == userId)
                .OrderByDescending(s => s.SavedAt)
                .ToList();

            var trainingIds = savedItems.Where(s => s.OpportunityType == OpportunityTypes.Training).Select(s => s.OpportunityId).ToList();
            var scholarshipIds = savedItems.Where(s => s.OpportunityType == OpportunityTypes.Scholarship).Select(s => s.OpportunityId).ToList();
            var eventIds = savedItems.Where(s => s.OpportunityType == OpportunityTypes.Event).Select(s => s.OpportunityId).ToList();

            var trainings = _context.TrainingPrograms
                .Include(t => t.Organization)
                .Where(t => trainingIds.Contains(t.Id))
                .ToList();

            var scholarships = _context.Scholarships
                .Include(s => s.Organization)
                .Where(s => scholarshipIds.Contains(s.Id))
                .ToList();

            var events = _context.AwarenessEvents
                .Include(e => e.Organization)
                .Where(e => eventIds.Contains(e.Id))
                .ToList();

            var model = new SavedOpportunitiesViewModel
            {
                CurrentTab = type ?? "All",
                SavedTrainings = trainings,
                SavedScholarships = scholarships,
                SavedEvents = events
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult MyRegistrations()
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please log in to view your registrations.";
                return RedirectToAction("Login", "Account");
            }

            SetUserInfo();
            var userId = GetCurrentUserId();

            var trainingRegistrations = _context.TrainingRegistrations
                .Include(r => r.TrainingProgram)
                    .ThenInclude(t => t.Organization)
                .Where(r => r.DisabilityUserId == userId)
                .OrderByDescending(r => r.RegisteredAt)
                .ToList();

            var scholarshipApps = _context.ScholarshipApplications
                .Include(a => a.Scholarship)
                    .ThenInclude(s => s.Organization)
                .Where(a => a.DisabilityUserId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToList();

            var eventRegistrations = _context.EventRegistrations
                .Include(r => r.AwarenessEvent)
                    .ThenInclude(e => e.Organization)
                .Where(r => r.DisabilityUserId == userId)
                .OrderByDescending(r => r.RegisteredAt)
                .ToList();

            var model = new MyRegistrationsViewModel
            {
                TrainingRegistrations = trainingRegistrations,
                ScholarshipApplications = scholarshipApps,
                EventRegistrations = eventRegistrations
            };

            return View(model);
        }
    }
}
