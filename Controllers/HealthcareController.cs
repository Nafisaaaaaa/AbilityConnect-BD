using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDP1.Controllers
{
    public class HealthcareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HealthcareController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        
        private bool IsLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole"));

        private bool IsDisability()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Disability";
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Admin";
        }

        private bool IsOrganization()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Organization";
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserId");

     
        public IActionResult Index(HealthcareFilterViewModel filter)
        {
            
            IQueryable<HealthcareProvider> query = _context.HealthcareProviders
                .Include(p => p.Organization);

     
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var term = filter.Keyword.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Specialization.ToLower().Contains(term) ||
                    p.OrganizationOrClinic.ToLower().Contains(term) ||
                    p.AvailableServices.ToLower().Contains(term) ||
                    p.District.ToLower().Contains(term) ||
                    p.City.ToLower().Contains(term) ||
                    p.Address.ToLower().Contains(term)
                );
            }

           
            if (!string.IsNullOrWhiteSpace(filter.ProviderType) && filter.ProviderType != "All")
            {
                query = query.Where(p => p.ProviderType == filter.ProviderType);
            }

           
            if (!string.IsNullOrWhiteSpace(filter.District) && filter.District != "All")
            {
                query = query.Where(p => p.District == filter.District);
            }

            
            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(p => p.City.ToLower().Contains(filter.City.Trim().ToLower()));
            }

            
            var providersList = query.ToList();

            var cards = providersList.Select(p =>
            {
                var services = (p.AvailableServices ?? "")
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                return new HealthcareProviderCardViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProviderType = p.ProviderType,
                    OrganizationOrClinic = p.OrganizationOrClinic,
                    Specialization = p.Specialization,
                    City = p.City,
                    District = p.District,
                    Address = p.Address,
                    ConsultationFee = p.ConsultationFee,
                    AvailabilitySchedule = p.AvailabilitySchedule,
                    ProfileImage = p.ProfileImage,
                    ServicesList = services
                };
            }).ToList();

            cards = filter.SortBy switch
            {
                "name_asc" => cards.OrderBy(c => c.Name).ToList(),
                "name_desc" => cards.OrderByDescending(c => c.Name).ToList(),
                _ => cards.OrderByDescending(c => c.Id).ToList()
            };

            filter.TotalCount = cards.Count;
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = 9;

            filter.Providers = cards
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            
            filter.AvailableProviderTypes = new List<string>
            {
                "All",
                "Doctor",
                "Physiotherapist",
                "Speech Therapist",
                "Rehabilitation Center",
                "Eye Specialist",
                "Mental Health Specialist"
            };

            filter.AvailableDistricts = _context.HealthcareProviders
                .Select(p => p.District)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return View(filter);
        }

        
        public IActionResult Details(int id)
        {
            var provider = _context.HealthcareProviders
                .Include(p => p.Organization)
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
            {
                TempData["ErrorMessage"] = "Healthcare provider not found.";
                return RedirectToAction(nameof(Index));
            }

            var services = (provider.AvailableServices ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            var apiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            var viewModel = new HealthcareProviderDetailsViewModel
            {
                Provider = provider,
                GoogleMapsApiKey = apiKey,
                ServicesList = services
            };

            if (IsDisability())
            {
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    viewModel.UserLatestAppointment = _context.HealthcareAppointments
                        .Where(a => a.HealthcareProviderId == id && a.DisabilityUserId == userId.Value)
                        .OrderByDescending(a => a.AppointmentDate)
                        .FirstOrDefault();
                }
            }

            return View(viewModel);
        }

        
        [HttpGet]
        public IActionResult BookAppointment(int id)
        {
            if (!IsDisability())
            {
                TempData["ErrorMessage"] = "Please log in as a Person with Disability to book an appointment.";
                return RedirectToAction("Login", "Account");
            }

            var provider = _context.HealthcareProviders.Find(id);
            if (provider == null)
            {
                TempData["ErrorMessage"] = "Healthcare provider not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new BookAppointmentViewModel
            {
                ProviderId = provider.Id,
                ProviderName = provider.Name,
                ProviderType = provider.ProviderType,
                OrganizationOrClinic = provider.OrganizationOrClinic,
                Specialization = provider.Specialization,
                AvailabilitySchedule = provider.AvailabilitySchedule,
                ConsultationFee = provider.ConsultationFee,
                Address = provider.Address,
                AppointmentDate = DateTime.Today.AddDays(1)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(int? id, BookAppointmentViewModel model)
        {
            if (!IsDisability())
            {
                TempData["ErrorMessage"] = "Please log in as a Person with Disability to book an appointment.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            if (model.ProviderId == 0 && id.HasValue)
            {
                model.ProviderId = id.Value;
            }

            var provider = _context.HealthcareProviders.Find(model.ProviderId);
            if (provider == null)
            {
                TempData["ErrorMessage"] = "Healthcare provider not found.";
                return RedirectToAction(nameof(Index));
            }

            
            if (model.AppointmentDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("AppointmentDate", "Appointment date cannot be in the past. Please select today or a future date.");
            }

            var bookingDateUtc = DateTime.SpecifyKind(model.AppointmentDate.Date, DateTimeKind.Utc);
            var nextDayUtc = bookingDateUtc.AddDays(1);

            var existingUserBooking = _context.HealthcareAppointments.FirstOrDefault(a =>
                a.HealthcareProviderId == model.ProviderId &&
                a.DisabilityUserId == userId.Value &&
                a.AppointmentDate >= bookingDateUtc && a.AppointmentDate < nextDayUtc &&
                a.TimeSlot == model.TimeSlot &&
                (a.Status == "Pending" || a.Status == "Confirmed")
            );

            if (existingUserBooking != null)
            {
                ModelState.AddModelError("", "You already have a pending or confirmed appointment with this provider for this date and time slot.");
            }

            if (!ModelState.IsValid)
            {
                model.ProviderName = provider.Name;
                model.ProviderType = provider.ProviderType;
                model.OrganizationOrClinic = provider.OrganizationOrClinic;
                model.Specialization = provider.Specialization;
                model.AvailabilitySchedule = provider.AvailabilitySchedule;
                model.ConsultationFee = provider.ConsultationFee;
                model.Address = provider.Address;
                if (model.PredefinedSlots == null || !model.PredefinedSlots.Any())
                {
                    model.PredefinedSlots = new BookAppointmentViewModel().PredefinedSlots;
                }
                return View(model);
            }

            var appointment = new HealthcareAppointment
            {
                HealthcareProviderId = model.ProviderId,
                DisabilityUserId = userId.Value,
                AppointmentDate = bookingDateUtc,
                TimeSlot = model.TimeSlot,
                ReasonForVisit = model.ReasonForVisit.Trim(),
                PatientNotes = model.PatientNotes?.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.HealthcareAppointments.Add(appointment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Your appointment request with {provider.Name} has been submitted! Status: Pending confirmation.";
            return RedirectToAction(nameof(MyAppointments));
        }

        
        public IActionResult MyAppointments()
        {
            if (!IsDisability())
            {
                TempData["ErrorMessage"] = "Please log in as a Person with Disability to view your appointments.";
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            var appointments = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .Where(a => a.DisabilityUserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.CreatedAt)
                .ToList();

            return View(appointments);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id)
        {
            if (!IsDisability())
                return Unauthorized();

            var userId = GetCurrentUserId();
            var appointment = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .FirstOrDefault(a => a.Id == id && a.DisabilityUserId == userId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(MyAppointments));
            }

            if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = $"Cannot cancel an appointment that is already {appointment.Status.ToLower()}.";
                return RedirectToAction(nameof(MyAppointments));
            }

            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Your appointment has been cancelled successfully.";
            return RedirectToAction(nameof(MyAppointments));
        }

       
        public IActionResult ManageAppointments(string? statusFilter, int? providerId)
        {
            if (!IsAdmin() && !IsOrganization())
            {
                TempData["ErrorMessage"] = "Access restricted to Healthcare Providers, Organizations, and Administrators.";
                return RedirectToAction("Login", "Account");
            }

            IQueryable<HealthcareAppointment> query = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .Include(a => a.DisabilityUser);

            if (IsOrganization())
            {
                var orgId = GetCurrentUserId();
                query = query.Where(a => a.HealthcareProvider != null && a.HealthcareProvider.OrganizationId == orgId);
            }

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
            ViewBag.IsAdmin = IsAdmin();

            return View(appointments);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAppointmentStatus(UpdateAppointmentStatusModel model)
        {
            if (!IsAdmin() && !IsOrganization())
                return Unauthorized();

            var appointment = _context.HealthcareAppointments
                .Include(a => a.HealthcareProvider)
                .FirstOrDefault(a => a.Id == model.AppointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction(nameof(ManageAppointments));
            }

           
            if (IsOrganization())
            {
                var orgId = GetCurrentUserId();
                if (appointment.HealthcareProvider?.OrganizationId != orgId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to manage appointments for this provider.";
                    return RedirectToAction(nameof(ManageAppointments));
                }
            }

            var allowedStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled", "Rejected" };
            if (!allowedStatuses.Contains(model.Status))
            {
                TempData["ErrorMessage"] = "Invalid appointment status.";
                return RedirectToAction(nameof(ManageAppointments));
            }

            appointment.Status = model.Status;
            if (!string.IsNullOrWhiteSpace(model.DoctorNotes))
            {
                appointment.DoctorNotes = model.DoctorNotes.Trim();
            }
            appointment.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Appointment status updated to '{model.Status}' successfully.";
            return RedirectToAction(nameof(ManageAppointments));
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin() && !IsOrganization())
            {
                TempData["ErrorMessage"] = "Access restricted to Administrators and Organizations.";
                return RedirectToAction("Login", "Account");
            }

            var model = new HealthcareProviderFormViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HealthcareProviderFormViewModel model)
        {
            if (!IsAdmin() && !IsOrganization())
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(model);

            int? orgId = null;
            if (IsOrganization())
            {
                orgId = GetCurrentUserId();
            }
            else if (IsAdmin() && model.OrganizationId.HasValue && model.OrganizationId.Value > 0)
            {
                orgId = model.OrganizationId.Value;
            }

            var provider = new HealthcareProvider
            {
                Name = model.Name.Trim(),
                ProviderType = model.ProviderType,
                OrganizationOrClinic = model.OrganizationOrClinic.Trim(),
                Description = model.Description.Trim(),
                Specialization = model.Specialization.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim().ToLower(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                District = model.District.Trim(),
                AvailableServices = model.AvailableServices.Trim(),
                ConsultationFee = model.ConsultationFee,
                AvailabilitySchedule = model.AvailabilitySchedule.Trim(),
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow
            };

            _context.HealthcareProviders.Add(provider);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Healthcare provider created successfully.";

            return RedirectToAction(nameof(Details), new { id = provider.Id });
        }

       
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin() && !IsOrganization())
                return Unauthorized();

            var provider = _context.HealthcareProviders.Find(id);
            if (provider == null)
            {
                TempData["ErrorMessage"] = "Provider not found.";
                return RedirectToAction(nameof(Index));
            }

            
            if (IsOrganization() && provider.OrganizationId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this provider.";
                return RedirectToAction(nameof(Index));
            }

            var model = new HealthcareProviderFormViewModel
            {
                Id = provider.Id,
                Name = provider.Name,
                ProviderType = provider.ProviderType,
                OrganizationOrClinic = provider.OrganizationOrClinic,
                Description = provider.Description,
                Specialization = provider.Specialization,
                Phone = provider.Phone,
                Email = provider.Email,
                Address = provider.Address,
                City = provider.City,
                District = provider.District,
                AvailableServices = provider.AvailableServices,
                ConsultationFee = provider.ConsultationFee,
                AvailabilitySchedule = provider.AvailabilitySchedule,
                OrganizationId = provider.OrganizationId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HealthcareProviderFormViewModel model)
        {
            if (!IsAdmin() && !IsOrganization())
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(model);

            var provider = _context.HealthcareProviders.Find(model.Id);
            if (provider == null)
            {
                TempData["ErrorMessage"] = "Provider not found.";
                return RedirectToAction(nameof(Index));
            }

            if (IsOrganization() && provider.OrganizationId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this provider.";
                return RedirectToAction(nameof(Index));
            }

            provider.Name = model.Name.Trim();
            provider.ProviderType = model.ProviderType;
            provider.OrganizationOrClinic = model.OrganizationOrClinic.Trim();
            provider.Description = model.Description.Trim();
            provider.Specialization = model.Specialization.Trim();
            provider.Phone = model.Phone.Trim();
            provider.Email = model.Email.Trim().ToLower();
            provider.Address = model.Address.Trim();
            provider.City = model.City.Trim();
            provider.District = model.District.Trim();
            provider.AvailableServices = model.AvailableServices.Trim();
            provider.ConsultationFee = model.ConsultationFee;
            provider.AvailabilitySchedule = model.AvailabilitySchedule.Trim();
            provider.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Healthcare provider updated successfully.";
            return RedirectToAction(nameof(Details), new { id = provider.Id });
        }
    }
}
