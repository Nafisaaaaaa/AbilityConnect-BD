using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using SDP1.Services;

namespace SDP1.Controllers
{
    public class AIRecommendationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;

        public AIRecommendationController(ApplicationDbContext context, IAIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        private void SetUserInfo()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SetUserInfo();

            var userRole = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            DisabilityUser? disabilityProfile = null;
            if (userRole == "Disability" && userId.HasValue)
            {
                disabilityProfile = await _context.DisabilityUsers.FirstOrDefaultAsync(u => u.Id == userId.Value);
                ViewBag.DisabilityProfile = disabilityProfile;
            }

            var model = new AIRecommendationResultViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetRecommendations(string query)
        {
            SetUserInfo();

            if (string.IsNullOrWhiteSpace(query))
            {
                ModelState.AddModelError("query", "Please enter what support or opportunity you are looking for.");
                return View("Index", new AIRecommendationResultViewModel());
            }

            query = query.Trim();
            if (query.Length > 500)
            {
                query = query.Substring(0, 500);
            }

            var nowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var lastRequestUnix = HttpContext.Session.GetInt32("LastAiRequestUnix");
            if (lastRequestUnix.HasValue)
            {
                var diff = nowUnix - lastRequestUnix.Value;
                if (diff >= 0 && diff < 2)
                {
                    ViewBag.CooldownMessage = "Please wait a few seconds before asking another question.";
                    return View("Index", new AIRecommendationResultViewModel
                    {
                        Query = query,
                        Summary = "Please wait a moment before sending another request."
                    });
                }
            }
            HttpContext.Session.SetInt32("LastAiRequestUnix", nowUnix);

            var userRole = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            DisabilityUser? disabilityProfile = null;

            if (userRole == "Disability" && userId.HasValue)
            {
                disabilityProfile = await _context.DisabilityUsers.FirstOrDefaultAsync(u => u.Id == userId.Value);
                ViewBag.DisabilityProfile = disabilityProfile;
            }


            var candidates = await GatherCandidateOpportunitiesAsync(query, disabilityProfile);

            var result = await _aiService.GetRecommendationsAsync(query, disabilityProfile, candidates);

            return View("Index", result);
        }

        private async Task<List<CandidateOpportunity>> GatherCandidateOpportunitiesAsync(
            string query,
            DisabilityUser? profile)
        {
            var candidates = new List<CandidateOpportunity>();
            var lowerQuery = query.ToLower();
            var queryWords = lowerQuery.Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Where(w => w.Length > 2)
                                       .Distinct()
                                       .ToList();

            var generalIntentWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "recommend", "recommendation", "recommendations", "suggest", "suggestion", "suggestions",
                "opportunity", "opportunities", "available", "suitable", "eligible", "help", "support",
                "option", "options", "what", "which", "where", "how", "give", "show", "find", "for", "with",
                "want", "need", "like", "best", "some", "any", "please", "can", "could", "would",
                "looking", "about", "there", "good"
            };

            var specificKeywords = queryWords.Where(w => !generalIntentWords.Contains(w)).ToList();

            bool lookingForJobs = lowerQuery.Contains("job") || lowerQuery.Contains("work") || lowerQuery.Contains("career")
                               || lowerQuery.Contains("remote") || lowerQuery.Contains("hiring") || lowerQuery.Contains("employment");

            bool lookingForTraining = lowerQuery.Contains("train") || lowerQuery.Contains("course") || lowerQuery.Contains("learn")
                                   || lowerQuery.Contains("freelanc") || lowerQuery.Contains("skill") || lowerQuery.Contains("computer");

            bool lookingForScholarship = lowerQuery.Contains("scholarship") || lowerQuery.Contains("stipend") || lowerQuery.Contains("fund")
                                      || lowerQuery.Contains("financial") || lowerQuery.Contains("education") || lowerQuery.Contains("tuition");

            bool lookingForHealthcare = lowerQuery.Contains("doctor") || lowerQuery.Contains("health") || lowerQuery.Contains("hospital")
                                     || lowerQuery.Contains("therap") || lowerQuery.Contains("clinic") || lowerQuery.Contains("medical")
                                     || lowerQuery.Contains("eye") || lowerQuery.Contains("physio");

            bool lookingForLearning = lowerQuery.Contains("video") || lowerQuery.Contains("hub") || lowerQuery.Contains("watch")
                                   || lowerQuery.Contains("braille") || lowerQuery.Contains("sign language") || lowerQuery.Contains("screen reader");

            bool lookingForEvent = lowerQuery.Contains("event") || lowerQuery.Contains("seminar") || lowerQuery.Contains("gathering")
                                || lowerQuery.Contains("workshop") || lowerQuery.Contains("awareness");

    
            bool searchAll = !lookingForJobs && !lookingForTraining && !lookingForScholarship && !lookingForHealthcare && !lookingForLearning && !lookingForEvent;

          
            if (lookingForJobs || searchAll)
            {
                var jobsQuery = _context.Jobs
                    .Include(j => j.Organization)
                    .Where(j => j.IsPublished && j.JobStatus == "Active");

                var jobs = await jobsQuery.ToListAsync();

                var matchedJobs = jobs.Where(j =>
                {
                    var text = $"{j.Title} {j.Description} {j.WorkplaceType} {j.Location} {j.RequiredSkills} {j.AccessibilityFeatures}".ToLower();
                    if (specificKeywords.Any())
                    {
                        return specificKeywords.Any(w => text.Contains(w));
                    }
                    return (profile != null && !string.IsNullOrWhiteSpace(profile.DisabilityType) && text.Contains(profile.DisabilityType.ToLower()))
                           || lookingForJobs;
                })
                .Take(5)
                .ToList();

                if (!matchedJobs.Any() && lookingForJobs && !specificKeywords.Any())
                {
                    matchedJobs = jobs.Take(3).ToList();
                }

                foreach (var j in matchedJobs)
                {
                    candidates.Add(new CandidateOpportunity
                    {
                        Id = j.Id,
                        Type = "Job",
                        Title = j.Title,
                        ProviderOrOrg = j.Organization?.OrganizationName ?? "AbilityConnect Partner",
                        Description = j.Description,
                        CategoryOrField = j.JobType + " • " + j.WorkplaceType,
                        Location = j.Location,
                        Details = $"Workplace: {j.WorkplaceType} | Skills: {j.RequiredSkills} | Deadline: {j.ApplicationDeadline:dd MMM yyyy}",
                        Url = $"/Jobs/Details/{j.Id}",
                        BadgeClass = "bg-primary",
                        IconClass = "fas fa-briefcase"
                    });
                }
            }

           
            if (lookingForTraining || searchAll)
            {
                var trainings = await _context.TrainingPrograms
                    .Include(t => t.Organization)
                    .Where(t => t.Status == "Active")
                    .ToListAsync();

                var matchedTrainings = trainings.Where(t =>
                {
                    var text = $"{t.Title} {t.Description} {t.TrainingCategory} {t.SkillsCovered} {t.DeliveryMode} {t.Eligibility}".ToLower();
                    if (specificKeywords.Any())
                    {
                        return specificKeywords.Any(w => text.Contains(w));
                    }
                    return (profile != null && !string.IsNullOrWhiteSpace(profile.Skills) && text.Contains(profile.Skills.ToLower()))
                           || lookingForTraining;
                })
                .Take(5)
                .ToList();

                if (!matchedTrainings.Any() && lookingForTraining && !specificKeywords.Any())
                {
                    matchedTrainings = trainings.Take(3).ToList();
                }

                foreach (var t in matchedTrainings)
                {
                    candidates.Add(new CandidateOpportunity
                    {
                        Id = t.Id,
                        Type = "Training Program",
                        Title = t.Title,
                        ProviderOrOrg = t.Organization?.OrganizationName ?? "Training Provider",
                        Description = t.Description,
                        CategoryOrField = t.TrainingCategory,
                        Location = t.Location + " (" + t.DeliveryMode + ")",
                        Details = $"Duration: {t.Duration} | Mode: {t.DeliveryMode} | Deadline: {t.RegistrationDeadline:dd MMM yyyy}",
                        Url = $"/Services/TrainingDetails/{t.Id}",
                        BadgeClass = "bg-success",
                        IconClass = "fas fa-graduation-cap"
                    });
                }
            }

            
            if (lookingForScholarship || searchAll)
            {
                var scholarships = await _context.Scholarships
                    .Include(s => s.Organization)
                    .Where(s => s.Status == "Active")
                    .ToListAsync();

                var matchedScholarships = scholarships.Where(s =>
                {
                    var text = $"{s.Title} {s.Description} {s.ScholarshipType} {s.FieldOfStudy} {s.Benefits} {s.Eligibility}".ToLower();
                    if (specificKeywords.Any())
                    {
                        return specificKeywords.Any(w => text.Contains(w));
                    }
                    return lookingForScholarship;
                })
                .Take(5)
                .ToList();

                if (!matchedScholarships.Any() && lookingForScholarship && !specificKeywords.Any())
                {
                    matchedScholarships = scholarships.Take(3).ToList();
                }

                foreach (var s in matchedScholarships)
                {
                    candidates.Add(new CandidateOpportunity
                    {
                        Id = s.Id,
                        Type = "Scholarship",
                        Title = s.Title,
                        ProviderOrOrg = s.Organization?.OrganizationName ?? "Scholarship Grantor",
                        Description = s.Description,
                        CategoryOrField = s.ScholarshipType + " • " + s.FieldOfStudy,
                        Location = s.Location,
                        Details = $"Type: {s.ScholarshipType} | Field: {s.FieldOfStudy} | Deadline: {s.ApplicationDeadline:dd MMM yyyy}",
                        Url = $"/Services/ScholarshipDetails/{s.Id}",
                        BadgeClass = "bg-warning text-dark",
                        IconClass = "fas fa-award"
                    });
                }
            }

            
            if (lookingForHealthcare || searchAll)
            {
                var providers = await _context.HealthcareProviders
                    .Include(hp => hp.Organization)
                    .ToListAsync();

                var matchedProviders = providers.Where(p =>
                {
                    var text = $"{p.Name} {p.Specialization} {p.ProviderType} {p.OrganizationOrClinic} {p.Description} {p.City}".ToLower();
                    if (specificKeywords.Any())
                    {
                        return specificKeywords.Any(w => text.Contains(w));
                    }
                    return (profile != null && !string.IsNullOrWhiteSpace(profile.Location) && text.Contains(profile.Location.ToLower()))
                           || lookingForHealthcare;
                })
                .Take(5)
                .ToList();

                if (!matchedProviders.Any() && lookingForHealthcare && !specificKeywords.Any())
                {
                    matchedProviders = providers.Take(3).ToList();
                }

                foreach (var p in matchedProviders)
                {
                    candidates.Add(new CandidateOpportunity
                    {
                        Id = p.Id,
                        Type = "Healthcare Provider",
                        Title = p.Name,
                        ProviderOrOrg = p.OrganizationOrClinic,
                        Description = p.Description,
                        CategoryOrField = p.ProviderType + " • " + p.Specialization,
                        Location = $"{p.City}, {p.District}",
                        Details = $"Type: {p.ProviderType} | Specialization: {p.Specialization} | Phone: {p.Phone}",
                        Url = $"/Healthcare/Details/{p.Id}",
                        BadgeClass = "bg-danger",
                        IconClass = "fas fa-user-md"
                    });
                }
            }

            
            if (lookingForLearning || searchAll)
            {
                var videos = await _context.LearningVideos
                    .Where(v => v.IsPublished)
                    .ToListAsync();

                var matchedVideos = videos.Where(v =>
                {
                    var text = $"{v.Title} {v.Description} {v.Category}".ToLower();
                    if (specificKeywords.Any())
                    {
                        return specificKeywords.Any(w => text.Contains(w));
                    }
                    return lookingForLearning;
                })
                .Take(4)
                .ToList();

                if (!matchedVideos.Any() && lookingForLearning && !specificKeywords.Any())
                {
                    matchedVideos = videos.Take(3).ToList();
                }

                foreach (var v in matchedVideos)
                {
                    candidates.Add(new CandidateOpportunity
                    {
                        Id = v.Id,
                        Type = "Learning Hub",
                        Title = v.Title,
                        ProviderOrOrg = "AbilityConnect Learning Hub",
                        Description = v.Description,
                        CategoryOrField = v.Category,
                        Location = "Online Video",
                        Details = $"Category: {v.Category} | Duration: {v.Duration}",
                        Url = $"/LearningHub/Watch/{v.Id}",
                        BadgeClass = "bg-info text-dark",
                        IconClass = "fas fa-book-reader"
                    });
                }
            }

            
            if (lookingForEvent || searchAll)
            {
                var today = DateTime.UtcNow.Date;
                var events = await _context.AwarenessEvents
                    .Include(e => e.Organization)
                    .Where(e => e.Status == "Active" && e.EventDate >= today)
                    .ToListAsync();

                var matchedEvents = events.Where(e =>
                {
                    var text = $"{e.Title} {e.Description} {e.EventType} {e.Location}".ToLower();
                    if (specificKeywords.Any())
                    {
                        return specificKeywords.Any(w => text.Contains(w));
                    }
                    return lookingForEvent;
                })
                .Take(4)
                .ToList();

                foreach (var e in matchedEvents)
                {
                    candidates.Add(new CandidateOpportunity
                    {
                        Id = e.Id,
                        Type = "Awareness Event",
                        Title = e.Title,
                        ProviderOrOrg = e.Organization?.OrganizationName ?? "Event Organizer",
                        Description = e.Description,
                        CategoryOrField = e.EventType,
                        Location = e.Location,
                        Details = $"Date: {e.EventDate:dd MMM yyyy} | Time: {e.StartTime} - {e.EndTime}",
                        Url = $"/Services/EventDetails/{e.Id}",
                        BadgeClass = "bg-secondary",
                        IconClass = "fas fa-calendar-alt"
                    });
                }
            }

            return candidates;
        }
    }
}
