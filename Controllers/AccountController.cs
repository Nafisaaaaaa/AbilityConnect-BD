using Microsoft.AspNetCore.Mvc;
using SDP1.Models;
using SDP1.Data;
using SDP1.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SDP1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public IActionResult Login()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(role))
                return RedirectToDashboard();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View(model);
            }

            var email = model.Email.ToLower().Trim();
            const string ADMIN_EMAIL = "admin@abilityconnect.com";

            
            if (email == ADMIN_EMAIL)
            {
                var admin = _context.Admins.FirstOrDefault(a => a.Email == email);

                if (admin != null && PasswordHelper.VerifyPassword(model.Password, admin.PasswordHash))
                {
                    HttpContext.Session.SetString("UserRole", "Admin");
                    HttpContext.Session.SetString("UserEmail", admin.Email);
                    HttpContext.Session.SetString("UserName", admin.FullName);
                    HttpContext.Session.SetInt32("UserId", admin.Id);
                    return RedirectToAction("Dashboard", "Admin");
                }

                if (admin != null && admin.PasswordHash == "Admin@123")
                {
                    admin.PasswordHash = PasswordHelper.HashPassword("Admin@123");
                    _context.SaveChanges();

                    HttpContext.Session.SetString("UserRole", "Admin");
                    HttpContext.Session.SetString("UserEmail", admin.Email);
                    HttpContext.Session.SetString("UserName", admin.FullName);
                    HttpContext.Session.SetInt32("UserId", admin.Id);
                    return RedirectToAction("Dashboard", "Admin");
                }

                ModelState.AddModelError("", "Invalid admin password");
                return View(model);
            }

            
            if (string.IsNullOrEmpty(model.Role))
            {
                ModelState.AddModelError("", "Please select your role");
                return View(model);
            }

           
            if (model.Role == "Disability")
            {
                var user = _context.DisabilityUsers.FirstOrDefault(u => u.Email == email);
                if (user != null && PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
                {
                    HttpContext.Session.SetString("UserRole", "Disability");
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserName", user.FullName);
                    HttpContext.Session.SetString("DisabilityType", user.DisabilityType);
                    HttpContext.Session.SetString("Location", user.Location);
                    HttpContext.Session.SetString("ProfilePicture", user.ProfilePicture ?? "");
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    return RedirectToAction("Dashboard", "Disability");
                }
                ModelState.AddModelError("", "Invalid email or password");
            }
           
            else if (model.Role == "Volunteer")
            {
                var volunteer = _context.Volunteers.FirstOrDefault(v => v.Email == email);

                if (volunteer == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                if (!PasswordHelper.VerifyPassword(model.Password, volunteer.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                
                if (!volunteer.IsVerified)
                {
                    TempData["ErrorMessage"] = "Your account verification is pending. You will receive an email notification once verification is complete.";
                    TempData["ShowToast"] = "true";
                    return RedirectToAction("Login");
                }

               
                HttpContext.Session.SetString("UserRole", "Volunteer");
                HttpContext.Session.SetString("UserEmail", volunteer.Email);
                HttpContext.Session.SetString("UserName", volunteer.FullName);
                HttpContext.Session.SetString("Location", volunteer.Location);
                HttpContext.Session.SetString("ProfilePicture", volunteer.ProfilePicture ?? "");
                HttpContext.Session.SetInt32("UserId", volunteer.Id);
                return RedirectToAction("Dashboard", "Volunteer");
            }
           
            else if (model.Role == "Organization")
            {
                var org = _context.Organizations.FirstOrDefault(o => o.Email == email);

                if (org == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                if (!PasswordHelper.VerifyPassword(model.Password, org.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

              
                if (!org.IsVerified)
                {
                    TempData["ErrorMessage"] = "Your account verification is pending. You will receive an email notification once verification is complete.";
                    TempData["ShowToast"] = "true";
                    return RedirectToAction("Login");
                }

               
                HttpContext.Session.SetString("UserRole", "Organization");
                HttpContext.Session.SetString("UserEmail", org.Email);
                HttpContext.Session.SetString("UserName", org.OrganizationName);
                HttpContext.Session.SetString("OrganizationType", org.OrganizationType);
                HttpContext.Session.SetString("Logo", org.Logo ?? "");
                HttpContext.Session.SetInt32("UserId", org.Id);
                return RedirectToAction("Dashboard", "Organization");
            }

            return View(model);
        }

      
        public IActionResult DisabilityRegister() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisabilityRegister(DisabilityRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            
            if (!PasswordHelper.IsValidFullName(model.FullName ?? "", out string nameError))
            {
                ModelState.AddModelError("FullName", nameError);
                return View(model);
            }

            
            var email = model.Email?.ToLower().Trim() ?? "";
            if (!PasswordHelper.IsValidEmail(email))
            {
                ModelState.AddModelError("Email", "Invalid email. Use lowercase only (e.g., name@example.com).");
                return View(model);
            }

            if (_context.DisabilityUsers.Any(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (!PasswordHelper.IsStrongPassword(model.Password ?? "", out string passError))
            {
                ModelState.AddModelError("Password", passError);
                return View(model);
            }

            if (model.Latitude.HasValue && (model.Latitude.Value < -90.0 || model.Latitude.Value > 90.0))
            {
                ModelState.AddModelError("Latitude", "Latitude must be between -90 and 90.");
                return View(model);
            }
            if (model.Longitude.HasValue && (model.Longitude.Value < -180.0 || model.Longitude.Value > 180.0))
            {
                ModelState.AddModelError("Longitude", "Longitude must be between -180 and 180.");
                return View(model);
            }

            string? profilePicture = null;
            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                profilePicture = await SaveFile(model.ProfilePictureFile, "profiles");
            }

            string? certificate = null;
            if (model.DisabilityCertificateFile != null && model.DisabilityCertificateFile.Length > 0)
            {
                certificate = await SaveFile(model.DisabilityCertificateFile, "certificates");
            }

            var user = new DisabilityUser
            {
                FullName = model.FullName ?? "",
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(model.Password ?? ""),
                PhoneNumber = model.PhoneNumber ?? "",
                DateOfBirth = model.DateOfBirth.HasValue
                    ? DateTime.SpecifyKind(model.DateOfBirth.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow,
                Gender = model.Gender ?? "",
                DisabilityType = model.DisabilityType ?? "",
                OtherDisabilityType = model.DisabilityType == "Other" ? model.OtherDisabilityType : null,
                Location = model.Location ?? "",
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Skills = model.Skills,
                Education = model.Education,
                Interests = model.Interests,
                ProfilePicture = profilePicture,
                DisabilityCertificate = certificate,
                RegisteredAt = DateTime.UtcNow,
                LastAction = "Registered"
            };

            _context.DisabilityUsers.Add(user);
            await _context.SaveChangesAsync();

            LogActivity("User Registered", $"{user.FullName} registered as Disability User", user.Id, user.FullName, "Disability");

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            TempData["ShowToast"] = "true";
            return RedirectToAction("Index", "Home");  
        }

        
        public IActionResult VolunteerRegister() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VolunteerRegister(VolunteerRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!PasswordHelper.IsValidFullName(model.FullName ?? "", out string nameError))
            {
                ModelState.AddModelError("FullName", nameError);
                return View(model);
            }

            var email = model.Email?.ToLower().Trim() ?? "";
            if (!PasswordHelper.IsValidEmail(email))
            {
                ModelState.AddModelError("Email", "Invalid email. Use lowercase only.");
                return View(model);
            }

            if (_context.Volunteers.Any(v => v.Email == email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (!PasswordHelper.IsStrongPassword(model.Password ?? "", out string passError))
            {
                ModelState.AddModelError("Password", passError);
                return View(model);
            }

            if (model.Latitude.HasValue && (model.Latitude.Value < -90.0 || model.Latitude.Value > 90.0))
            {
                ModelState.AddModelError("Latitude", "Latitude must be between -90 and 90.");
                return View(model);
            }
            if (model.Longitude.HasValue && (model.Longitude.Value < -180.0 || model.Longitude.Value > 180.0))
            {
                ModelState.AddModelError("Longitude", "Longitude must be between -180 and 180.");
                return View(model);
            }

            string? profilePicture = null;
            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                profilePicture = await SaveFile(model.ProfilePictureFile, "profiles");
            }

            var volunteer = new Volunteer
            {
                FullName = model.FullName ?? "",
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(model.Password ?? ""),
                PhoneNumber = model.PhoneNumber ?? "",
                Location = model.Location ?? "",
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Skills = model.Skills ?? "",
                Availability = model.Availability,
                Experience = model.Experience,
                LanguagesSpoken = model.LanguagesSpoken,
                ProfilePicture = profilePicture,
                RegisteredAt = DateTime.UtcNow,
                IsVerified = false,
                LastAction = "Registered"
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            LogActivity("Volunteer Registered", $"{volunteer.FullName} registered as Volunteer", volunteer.Id, volunteer.FullName, "Volunteer");

            TempData["SuccessMessage"] = "Registration successful! Please wait for admin verification. Login will be enabled after verification.";
            TempData["ShowToast"] = "true";
            return RedirectToAction("Index", "Home");  
        }

        
        public IActionResult OrganizationRegister() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrganizationRegister(OrganizationRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email?.ToLower().Trim() ?? "";
            if (!PasswordHelper.IsValidEmail(email))
            {
                ModelState.AddModelError("Email", "Invalid email. Use lowercase only.");
                return View(model);
            }

            if (_context.Organizations.Any(o => o.Email == email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (!PasswordHelper.IsStrongPassword(model.Password ?? "", out string passError))
            {
                ModelState.AddModelError("Password", passError);
                return View(model);
            }

            if (model.Latitude.HasValue && (model.Latitude.Value < -90.0 || model.Latitude.Value > 90.0))
            {
                ModelState.AddModelError("Latitude", "Latitude must be between -90 and 90.");
                return View(model);
            }
            if (model.Longitude.HasValue && (model.Longitude.Value < -180.0 || model.Longitude.Value > 180.0))
            {
                ModelState.AddModelError("Longitude", "Longitude must be between -180 and 180.");
                return View(model);
            }

            string? logo = null;
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                logo = await SaveFile(model.LogoFile, "organizations");
            }

            var addressVal = model.Address?.Trim() ?? "";
            string cityVal = !string.IsNullOrWhiteSpace(model.City) ? model.City.Trim() : (addressVal.Length > 0 ? addressVal : "Dhaka");
            string divisionVal = !string.IsNullOrWhiteSpace(model.Division) ? model.Division.Trim() : (addressVal.Length > 0 ? addressVal : "Dhaka");

            var org = new Organization
            {
                OrganizationName = model.OrganizationName ?? "",
                OrganizationType = model.OrganizationType ?? "",
                OtherOrganizationType = model.OrganizationType == "Other" ? model.OtherOrganizationType : null,
                OrganizationDescription = model.OrganizationDescription ?? "",
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(model.Password ?? ""),
                PhoneNumber = model.PhoneNumber ?? "",
                Address = addressVal,
                City = cityVal,
                Division = divisionVal,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Website = model.Website,
                FacebookProfileLink = model.FacebookProfileLink,
                ContactPersonName = model.ContactPersonName ?? "",
                ContactPersonDesignation = model.ContactPersonDesignation ?? "",
                ContactPersonPhone = model.ContactPersonPhone ?? "",
                ContactPersonEmail = model.ContactPersonEmail ?? "",
                Logo = logo,
                IsVerified = false,
                RegisteredAt = DateTime.UtcNow,
                LastAction = "Registered"
            };

            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            LogActivity("Organization Registered", $"{org.OrganizationName} registered", org.Id, org.OrganizationName, "Organization");

            TempData["SuccessMessage"] = "Registration successful! Please wait for admin verification. Login will be enabled after verification.";
            TempData["ShowToast"] = "true";
            return RedirectToAction("Index", "Home");  
        }

      
        [HttpGet]
        public IActionResult CheckEmailAvailability(string email, string role)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { available = false, message = "Email is required" });

            email = email.ToLower().Trim();

            if (email == "admin@abilityconnect.com")
                return Json(new { available = false, message = "This email is reserved" });

            if (!PasswordHelper.IsValidEmail(email))
                return Json(new { available = false, message = "Invalid email format" });

            bool exists = role switch
            {
                "Disability" => _context.DisabilityUsers.Any(u => u.Email == email),
                "Volunteer" => _context.Volunteers.Any(v => v.Email == email),
                "Organization" => _context.Organizations.Any(o => o.Email == email),
                _ => false
            };

            if (exists)
                return Json(new { available = false, message = "Email is already registered" });

            return Json(new { available = true, message = "Email is available" });
        }

        
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required");
                return View();
            }

            email = email.ToLower().Trim();
            var token = Guid.NewGuid().ToString();
            var expiry = DateTime.UtcNow.AddHours(1);

            var dis = _context.DisabilityUsers.FirstOrDefault(u => u.Email == email);
            var vol = _context.Volunteers.FirstOrDefault(v => v.Email == email);
            var org = _context.Organizations.FirstOrDefault(o => o.Email == email);
            var adm = _context.Admins.FirstOrDefault(a => a.Email == email);

            if (dis == null && vol == null && org == null && adm == null)
            {
                TempData["InfoMessage"] = "If the email is registered, you will receive a reset link.";
                return View();
            }

            if (dis != null) { dis.ResetToken = token; dis.ResetTokenExpiry = expiry; }
            if (vol != null) { vol.ResetToken = token; vol.ResetTokenExpiry = expiry; }
            if (org != null) { org.ResetToken = token; org.ResetTokenExpiry = expiry; }

            _context.SaveChanges();

            
            TempData["ResetLink"] = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);
            TempData["SuccessMessage"] = "Reset link generated. Copy it to reset password.";

            return RedirectToAction("ResetPassword", new { token });
        }

        
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var dis = _context.DisabilityUsers.FirstOrDefault(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
            var vol = _context.Volunteers.FirstOrDefault(v => v.ResetToken == token && v.ResetTokenExpiry > DateTime.UtcNow);
            var org = _context.Organizations.FirstOrDefault(o => o.ResetToken == token && o.ResetTokenExpiry > DateTime.UtcNow);

            if (dis == null && vol == null && org == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                ViewBag.Token = token;
                return View();
            }

            if (!PasswordHelper.IsStrongPassword(newPassword, out string passError))
            {
                ModelState.AddModelError("", passError);
                ViewBag.Token = token;
                return View();
            }

            var dis = _context.DisabilityUsers.FirstOrDefault(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
            var vol = _context.Volunteers.FirstOrDefault(v => v.ResetToken == token && v.ResetTokenExpiry > DateTime.UtcNow);
            var org = _context.Organizations.FirstOrDefault(o => o.ResetToken == token && o.ResetTokenExpiry > DateTime.UtcNow);

            if (dis == null && vol == null && org == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            var newHash = PasswordHelper.HashPassword(newPassword);

            if (dis != null)
            {
                dis.PasswordHash = newHash;
                dis.ResetToken = null;
                dis.ResetTokenExpiry = null;
                dis.UpdatedAt = DateTime.UtcNow;
                dis.LastPasswordChangedAt = DateTime.UtcNow;
                dis.LastAction = "Password Reset";
            }
            if (vol != null)
            {
                vol.PasswordHash = newHash;
                vol.ResetToken = null;
                vol.ResetTokenExpiry = null;
                vol.UpdatedAt = DateTime.UtcNow;
                vol.LastPasswordChangedAt = DateTime.UtcNow;
                vol.LastAction = "Password Reset";
            }
            if (org != null)
            {
                org.PasswordHash = newHash;
                org.ResetToken = null;
                org.ResetTokenExpiry = null;
                org.UpdatedAt = DateTime.UtcNow;
                org.LastPasswordChangedAt = DateTime.UtcNow;
                org.LastAction = "Password Reset";
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Password reset successfully! Please login.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword() => RedirectToAction("ChangePassword", "Profile");

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out.";
            TempData["ShowToast"] = "true";
            return RedirectToAction("Login");
        }

        
        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{folder}/{fileName}";
        }

        private void LogActivity(string actionType, string description, int? targetUserId = null, string? targetUserName = null, string? targetUserType = null)
        {
            _context.AdminActivityLogs.Add(new AdminActivityLog
            {
                ActionType = actionType,
                Description = description,
                TargetUserId = targetUserId,
                TargetUserName = targetUserName,
                TargetUserType = targetUserType,
                Timestamp = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        private IActionResult RedirectToDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Disability" => RedirectToAction("Dashboard", "Disability"),
                "Volunteer" => RedirectToAction("Dashboard", "Volunteer"),
                "Organization" => RedirectToAction("Dashboard", "Organization"),
                _ => RedirectToAction("Login")
            };
        }
    }
}