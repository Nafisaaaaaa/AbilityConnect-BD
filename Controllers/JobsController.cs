using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SDP1.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private readonly string[] _permittedExtensions = { ".pdf", ".doc", ".docx" };
        private readonly string[] _permittedMimeTypes = {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        private const long MaxFileSize = 5 * 1024 * 1024; 

        public JobsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        #region Session / Role Helpers

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetUserRole() => HttpContext.Session.GetString("UserRole");
        private bool IsDisabilityUser() => GetUserRole() == "Disability";
        private bool IsOrganization() => GetUserRole() == "Organization";
        private bool IsAdmin() => GetUserRole() == "Admin";

        #endregion

        
        public async Task<IActionResult> Index(
            string? search,
            string? jobType,
            string? workplaceType,
            string? location,
            string? accessibility,
            bool? flexibleHours,
            bool? assistiveTech,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 9;
            if (page < 1) page = 1;

            var query = _context.Jobs
                .Include(j => j.Organization)
                .Where(j => j.IsPublished && j.JobStatus == "Active");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(j =>
                    j.Title.ToLower().Contains(term) ||
                    (j.Organization != null && j.Organization.OrganizationName.ToLower().Contains(term)) ||
                    j.Location.ToLower().Contains(term) ||
                    j.RequiredSkills.ToLower().Contains(term) ||
                    (j.AccessibilityFeatures != null && j.AccessibilityFeatures.ToLower().Contains(term)));
            }

           
            if (!string.IsNullOrWhiteSpace(jobType))
            {
                query = query.Where(j => j.JobType == jobType);
            }

            if (!string.IsNullOrWhiteSpace(workplaceType))
            {
                query = query.Where(j => j.WorkplaceType == workplaceType);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(j => j.Location.ToLower().Contains(location.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(accessibility))
            {
                var accTerm = accessibility.Trim().ToLower();
                query = query.Where(j => j.AccessibilityFeatures != null && j.AccessibilityFeatures.ToLower().Contains(accTerm));
            }

            if (flexibleHours.HasValue && flexibleHours.Value)
            {
                query = query.Where(j => j.FlexibleWorkingHours);
            }

            if (assistiveTech.HasValue && assistiveTech.Value)
            {
                query = query.Where(j => j.AssistiveTechnologySupport);
            }

            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "newest" : sortOrder;
            query = sortOrder switch
            {
                "oldest" => query.OrderBy(j => j.CreatedAt),
                "deadline" => query.OrderBy(j => j.ApplicationDeadline),
                "title" => query.OrderBy(j => j.Title),
                _ => query.OrderByDescending(j => j.CreatedAt)
            };

          
            var totalJobs = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalJobs / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var jobs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new JobListViewModel
            {
                Jobs = jobs,
                SearchKeyword = search,
                JobType = jobType,
                WorkplaceType = workplaceType,
                Location = location,
                AccessibilityFeature = accessibility,
                FlexibleHoursOnly = flexibleHours,
                AssistiveTechOnly = assistiveTech,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalJobs = totalJobs,
                PageSize = pageSize
            };

            
            var userId = GetUserId();
            if (userId.HasValue && IsDisabilityUser())
            {
                var appliedApps = await _context.JobApplications
                    .Where(ja => ja.DisabilityUserId == userId.Value)
                    .Select(ja => new { ja.JobId, ja.Status })
                    .ToListAsync();

                viewModel.AppliedJobIds = new HashSet<int>(appliedApps.Select(a => a.JobId));
                viewModel.ApplicationStatuses = appliedApps.ToDictionary(a => a.JobId, a => a.Status);

                var savedIds = await _context.SavedJobs
                    .Where(sj => sj.DisabilityUserId == userId.Value)
                    .Select(sj => sj.JobId)
                    .ToListAsync();
                viewModel.SavedJobIds = new HashSet<int>(savedIds);
            }

           
            viewModel.AvailableLocations = await _context.Jobs
                .Where(j => j.IsPublished && j.JobStatus == "Active")
                .Select(j => j.Location)
                .Distinct()
                .Take(20)
                .ToListAsync();

            viewModel.AvailableJobTypes = new List<string> { "Full-time", "Part-time", "Contract", "Internship", "Volunteer" };
            viewModel.AvailableWorkplaceTypes = new List<string> { "Onsite", "Remote", "Hybrid" };

            return View(viewModel);
        }

       
        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Organization)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            var userId = GetUserId();
            var viewModel = new JobDetailsViewModel
            {
                Job = job,
                IsDisabilityUser = IsDisabilityUser(),
                IsAdmin = IsAdmin(),
                IsOwner = userId.HasValue && IsOrganization() && job.OrganizationId == userId.Value
            };

            if (userId.HasValue && IsDisabilityUser())
            {
                viewModel.Application = await _context.JobApplications
                    .FirstOrDefaultAsync(ja => ja.JobId == id && ja.DisabilityUserId == userId.Value);
                viewModel.HasApplied = viewModel.Application != null;

                viewModel.IsSaved = await _context.SavedJobs
                    .AnyAsync(sj => sj.JobId == id && sj.DisabilityUserId == userId.Value);
            }

            return View(viewModel);
        }

        
        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Person with Disability to apply for jobs.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;

            var job = await _context.Jobs
                .Include(j => j.Organization)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            if (!job.IsPublished || job.JobStatus != "Active")
            {
                TempData["ErrorMessage"] = "This job posting is no longer accepting applications.";
                return RedirectToAction("Details", new { id });
            }

            if (job.ApplicationDeadline.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "The application deadline for this job has expired.";
                return RedirectToAction("Details", new { id });
            }

           
            var existingApp = await _context.JobApplications
                .FirstOrDefaultAsync(ja => ja.JobId == id && ja.DisabilityUserId == userId);

            if (existingApp != null)
            {
                TempData["InfoMessage"] = $"You have already applied for this job on {existingApp.AppliedAt:MMM dd, yyyy}. Status: {existingApp.Status}.";
                return RedirectToAction("Details", new { id });
            }

            
            var user = await _context.DisabilityUsers.FindAsync(userId);
            var model = new JobApplyViewModel
            {
                JobId = job.Id,
                JobTitle = job.Title,
                OrganizationName = job.Organization?.OrganizationName ?? "Organization",
                WorkplaceType = job.WorkplaceType,
                Location = job.Location,
                ApplicantFullName = user?.FullName ?? "",
                ApplicantEmail = user?.Email ?? "",
                ApplicantPhone = user?.PhoneNumber ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(JobApplyViewModel model)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Person with Disability to apply for jobs.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;

            var job = await _context.Jobs
                .Include(j => j.Organization)
                .FirstOrDefaultAsync(j => j.Id == model.JobId);

            if (job == null) return NotFound();

            if (!job.IsPublished || job.JobStatus != "Active")
            {
                TempData["ErrorMessage"] = "This job posting is no longer accepting applications.";
                return RedirectToAction("Details", new { id = model.JobId });
            }

            var duplicate = await _context.JobApplications
                .AnyAsync(ja => ja.JobId == model.JobId && ja.DisabilityUserId == userId);

            if (duplicate)
            {
                TempData["ErrorMessage"] = "You have already submitted an application for this job.";
                return RedirectToAction("Details", new { id = model.JobId });
            }

            if (model.ResumeFile == null || model.ResumeFile.Length == 0)
            {
                ModelState.AddModelError("ResumeFile", "Please select a resume file to upload.");
            }
            else
            {
                if (model.ResumeFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("ResumeFile", "Resume file size cannot exceed 5 MB.");
                }

                var extension = Path.GetExtension(model.ResumeFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ResumeFile", "Invalid file type. Only PDF (.pdf), Word (.doc, .docx) files are allowed.");
                }

                if (!_permittedMimeTypes.Contains(model.ResumeFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError("ResumeFile", "Invalid file content type.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.JobTitle = job.Title;
                model.OrganizationName = job.Organization?.OrganizationName ?? "Organization";
                model.WorkplaceType = job.WorkplaceType;
                model.Location = job.Location;
                return View(model);
            }

            var extensionSafe = Path.GetExtension(model.ResumeFile!.FileName).ToLowerInvariant();
            var uniqueFileName = $"resume_{userId}_{Guid.NewGuid():N}{extensionSafe}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ResumeFile.CopyToAsync(stream);
            }

            var application = new JobApplication
            {
                JobId = model.JobId,
                DisabilityUserId = userId,
                ApplicantFullName = model.ApplicantFullName.Trim(),
                ApplicantEmail = model.ApplicantEmail.Trim(),
                ApplicantPhone = model.ApplicantPhone.Trim(),
                ResumePath = "/uploads/resumes/" + uniqueFileName,
                ResumeFileName = Path.GetFileName(model.ResumeFile.FileName),
                CoverLetter = model.CoverLetter?.Trim(),
                AccommodationRequested = model.AccommodationRequested?.Trim(),
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your job application has been submitted successfully!";
            return RedirectToAction("MyApplications");
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSave(int id, string? returnUrl)
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "You must be logged in as a Person with Disability to save jobs.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            var existingSave = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.JobId == id && sj.DisabilityUserId == userId);

            bool isSaved;
            if (existingSave != null)
            {
                _context.SavedJobs.Remove(existingSave);
                await _context.SaveChangesAsync();
                isSaved = false;
                TempData["SuccessMessage"] = "Job removed from your saved list.";
            }
            else
            {
                _context.SavedJobs.Add(new SavedJob
                {
                    JobId = id,
                    DisabilityUserId = userId,
                    SavedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                isSaved = true;
                TempData["SuccessMessage"] = "Job saved to your bookmarks!";
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, isSaved, message = TempData["SuccessMessage"] });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", new { id });
        }

      
        public async Task<IActionResult> SavedJobs()
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please login to view saved jobs.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var saved = await _context.SavedJobs
                .Include(sj => sj.Job)
                    .ThenInclude(j => j!.Organization)
                .Where(sj => sj.DisabilityUserId == userId)
                .OrderByDescending(sj => sj.SavedAt)
                .ToListAsync();

           
            var appliedJobIds = await _context.JobApplications
                .Where(ja => ja.DisabilityUserId == userId)
                .Select(ja => ja.JobId)
                .ToListAsync();

            ViewBag.AppliedJobIds = new HashSet<int>(appliedJobIds);

            return View(saved);
        }

        
        public async Task<IActionResult> MyApplications()
        {
            if (!IsDisabilityUser())
            {
                TempData["ErrorMessage"] = "Please login to view your applications.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var applications = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Organization)
                .Where(ja => ja.DisabilityUserId == userId)
                .OrderByDescending(ja => ja.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

    
        public async Task<IActionResult> Manage()
        {
            if (!IsOrganization() && !IsAdmin())
            {
                TempData["ErrorMessage"] = "Access restricted to Organizations and Administrators.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            IQueryable<Job> query = _context.Jobs
                .Include(j => j.Organization)
                .Include(j => j.JobApplications);

            if (IsOrganization())
            {
                query = query.Where(j => j.OrganizationId == userId);
            }

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var viewModel = new OrgJobManageViewModel
            {
                Jobs = jobs.Select(j => new OrgJobItemViewModel
                {
                    Job = j,
                    ApplicationCount = j.JobApplications.Count,
                    PendingCount = j.JobApplications.Count(a => a.Status == "Pending"),
                    ShortlistedCount = j.JobApplications.Count(a => a.Status == "Shortlisted")
                }).ToList(),
                TotalJobs = jobs.Count,
                ActiveJobs = jobs.Count(j => j.JobStatus == "Active"),
                TotalApplicants = jobs.Sum(j => j.JobApplications.Count)
            };

            return View(viewModel);
        }

      
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsOrganization() && !IsAdmin())
            {
                TempData["ErrorMessage"] = "Only verified organizations can post jobs.";
                return RedirectToAction("Login", "Account");
            }

            var model = new JobCreateViewModel
            {
                ApplicationDeadline = DateTime.Today.AddDays(30)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobCreateViewModel model)
        {
            if (!IsOrganization() && !IsAdmin())
            {
                TempData["ErrorMessage"] = "Access restricted.";
                return RedirectToAction("Login", "Account");
            }

            if (model.ApplicationDeadline.Date < DateTime.Today)
            {
                ModelState.AddModelError("ApplicationDeadline", "Application deadline must be today or a future date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetUserId()!.Value;

            var job = new Job
            {
                OrganizationId = userId,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                JobType = model.JobType,
                WorkplaceType = model.WorkplaceType,
                Location = model.Location.Trim(),
                Salary = model.Salary?.Trim(),
                AccessibilityFeatures = model.AccessibilityFeatures?.Trim(),
                FlexibleWorkingHours = model.FlexibleWorkingHours,
                AssistiveTechnologySupport = model.AssistiveTechnologySupport,
                AssistiveTechDetails = model.AssistiveTechDetails?.Trim(),
                RequiredSkills = model.RequiredSkills.Trim(),
                EducationRequirement = model.EducationRequirement?.Trim(),
                ApplicationDeadline = DateTime.SpecifyKind(model.ApplicationDeadline, DateTimeKind.Utc),
                VacancyCount = model.VacancyCount,
                ExperienceLevel = model.ExperienceLevel,
                JobStatus = model.IsPublished ? "Active" : "Draft",
                IsPublished = model.IsPublished,
                CreatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job posting created successfully!";
            return RedirectToAction("Manage");
        }

       
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            if (IsAdmin())
            {
                TempData["ErrorMessage"] = "Administrators are not permitted to edit job postings. Only the posting organization may edit their jobs.";
                return RedirectToAction("JobsAndPrograms", "Admin");
            }

            var userId = GetUserId();
            if (!IsOrganization() || job.OrganizationId != userId)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this job post.";
                return RedirectToAction("Manage");
            }

            var model = new JobEditViewModel
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                JobType = job.JobType,
                WorkplaceType = job.WorkplaceType,
                Location = job.Location,
                Salary = job.Salary,
                AccessibilityFeatures = job.AccessibilityFeatures,
                FlexibleWorkingHours = job.FlexibleWorkingHours,
                AssistiveTechnologySupport = job.AssistiveTechnologySupport,
                AssistiveTechDetails = job.AssistiveTechDetails,
                RequiredSkills = job.RequiredSkills,
                EducationRequirement = job.EducationRequirement,
                ApplicationDeadline = job.ApplicationDeadline,
                VacancyCount = job.VacancyCount,
                ExperienceLevel = job.ExperienceLevel,
                JobStatus = job.JobStatus,
                IsPublished = job.IsPublished
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobEditViewModel model)
        {
            var job = await _context.Jobs.FindAsync(model.Id);
            if (job == null) return NotFound();

            if (IsAdmin())
            {
                TempData["ErrorMessage"] = "Administrators are not permitted to edit job postings. Only the posting organization may edit their jobs.";
                return RedirectToAction("JobsAndPrograms", "Admin");
            }

            var userId = GetUserId();
            if (!IsOrganization() || job.OrganizationId != userId)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this job post.";
                return RedirectToAction("Manage");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            job.Title = model.Title.Trim();
            job.Description = model.Description.Trim();
            job.JobType = model.JobType;
            job.WorkplaceType = model.WorkplaceType;
            job.Location = model.Location.Trim();
            job.Salary = model.Salary?.Trim();
            job.AccessibilityFeatures = model.AccessibilityFeatures?.Trim();
            job.FlexibleWorkingHours = model.FlexibleWorkingHours;
            job.AssistiveTechnologySupport = model.AssistiveTechnologySupport;
            job.AssistiveTechDetails = model.AssistiveTechDetails?.Trim();
            job.RequiredSkills = model.RequiredSkills.Trim();
            job.EducationRequirement = model.EducationRequirement?.Trim();
            job.ApplicationDeadline = DateTime.SpecifyKind(model.ApplicationDeadline, DateTimeKind.Utc);
            job.VacancyCount = model.VacancyCount;
            job.ExperienceLevel = model.ExperienceLevel;
            job.JobStatus = model.JobStatus;
            job.IsPublished = model.IsPublished;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job posting updated successfully!";
            return RedirectToAction("Manage");
        }

        
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Organization)
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            var userId = GetUserId();
            if (!IsAdmin() && (!IsOrganization() || job.OrganizationId != userId))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this job post.";
                return RedirectToAction("Manage");
            }

            return View(job);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            var userId = GetUserId();
            if (!IsAdmin() && (!IsOrganization() || job.OrganizationId != userId))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this job post.";
                return RedirectToAction("Manage");
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job post deleted successfully.";
            return RedirectToAction("Manage");
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            var userId = GetUserId();
            if (!IsAdmin() && (!IsOrganization() || job.OrganizationId != userId))
            {
                TempData["ErrorMessage"] = "Permission denied.";
                return RedirectToAction("Manage");
            }

            job.JobStatus = job.JobStatus == "Active" ? "Closed" : "Active";
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Job status changed to {job.JobStatus}.";
            return RedirectToAction("Manage");
        }

       
        public async Task<IActionResult> Applicants(int id, string? statusFilter)
        {
            var job = await _context.Jobs
                .Include(j => j.Organization)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            var userId = GetUserId();
            if (!IsAdmin() && (!IsOrganization() || job.OrganizationId != userId))
            {
                TempData["ErrorMessage"] = "Permission denied.";
                return RedirectToAction("Manage");
            }

            var appQuery = _context.JobApplications
                .Include(a => a.DisabilityUser)
                .Where(a => a.JobId == id);

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                appQuery = appQuery.Where(a => a.Status == statusFilter);
            }

            var applications = await appQuery
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var model = new JobApplicantsViewModel
            {
                Job = job,
                Applications = applications,
                StatusFilter = statusFilter
            };

            return View(model);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicantStatus(int applicationId, string status, string? notes)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null) return NotFound();

            var userId = GetUserId();
            if (!IsAdmin() && (!IsOrganization() || application.Job?.OrganizationId != userId))
            {
                TempData["ErrorMessage"] = "Permission denied.";
                return RedirectToAction("Manage");
            }

            var validStatuses = new[] { "Pending", "Reviewed", "Shortlisted", "Rejected", "Accepted" };
            if (!validStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid application status.";
                return RedirectToAction("Applicants", new { id = application.JobId });
            }

            application.Status = status;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                application.OrganizationNotes = notes.Trim();
            }
            application.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Applicant status updated to '{status}'.";
            return RedirectToAction("Applicants", new { id = application.JobId });
        }

        public async Task<IActionResult> ReviewApplications(string? statusFilter)
        {
            if (!IsOrganization() && !IsAdmin())
            {
                TempData["ErrorMessage"] = "Access restricted to Organizations and Administrators.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetUserId()!.Value;
            var query = _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.DisabilityUser)
                .AsQueryable();

            if (IsOrganization())
            {
                query = query.Where(ja => ja.Job != null && ja.Job.OrganizationId == userId);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(ja => ja.Status == statusFilter);
            }

            var applications = await query
                .OrderByDescending(ja => ja.AppliedAt)
                .ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            return View(applications);
        }
    }
}
