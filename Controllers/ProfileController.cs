using Microsoft.AspNetCore.Mvc;
using SDP1.Data;
using SDP1.Helpers;
using SDP1.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SDP1.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetRole() => HttpContext.Session.GetString("UserRole");
        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");

        
        public IActionResult Edit()
        {
            var role = GetRole();
            var userId = GetUserId();

            if (string.IsNullOrEmpty(role) || userId == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Role = role;

            if (role == "Disability")
            {
                var user = _context.DisabilityUsers.Find(userId);
                if (user == null) return NotFound();
                return View("EditDisability", user);
            }
            else if (role == "Volunteer")
            {
                var vol = _context.Volunteers.Find(userId);
                if (vol == null) return NotFound();
                return View("EditVolunteer", vol);
            }
            else if (role == "Organization")
            {
                var org = _context.Organizations.Find(userId);
                if (org == null) return NotFound();
                return View("EditOrganization", org);
            }

            return RedirectToAction("Dashboard", "Home");
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDisability(DisabilityUser model, IFormFile? ProfilePictureFile, IFormFile? DisabilityCertificateFile)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (role != "Disability" || userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.DisabilityUsers.Find(userId);
            if (user == null) return NotFound();

            if (!PasswordHelper.IsValidFullName(model.FullName ?? "", out string nameError))
            {
                ModelState.AddModelError("FullName", nameError);
                return View(model);
            }

            user.FullName = model.FullName ?? "";
            user.PhoneNumber = model.PhoneNumber ?? "";
            user.DisabilityType = model.DisabilityType ?? "";
            user.Location = model.Location ?? "";
            user.Latitude = model.Latitude;
            user.Longitude = model.Longitude;
            user.Gender = model.Gender;
            user.Skills = model.Skills;
            user.Education = model.Education;
            user.Interests = model.Interests;

            if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
                user.ProfilePicture = await SaveFile(ProfilePictureFile, "profiles");

            if (DisabilityCertificateFile != null && DisabilityCertificateFile.Length > 0)
                user.DisabilityCertificate = await SaveFile(DisabilityCertificateFile, "certificates");

            user.UpdatedAt = DateTime.UtcNow;
            user.LastAction = "Profile Updated";

            _context.SaveChanges();
            LogActivity("Profile Updated", $"{user.FullName} updated their profile", user.Id, user.FullName, "Disability");

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Dashboard", "Disability");
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVolunteer(Volunteer model, IFormFile? ProfilePictureFile)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (role != "Volunteer" || userId == null)
                return RedirectToAction("Login", "Account");

            var vol = _context.Volunteers.Find(userId);
            if (vol == null) return NotFound();

            if (!PasswordHelper.IsValidFullName(model.FullName ?? "", out string nameError))
            {
                ModelState.AddModelError("FullName", nameError);
                return View(model);
            }

            vol.FullName = model.FullName ?? "";
            vol.PhoneNumber = model.PhoneNumber ?? "";
            vol.Location = model.Location ?? "";
            vol.Latitude = model.Latitude;
            vol.Longitude = model.Longitude;
            vol.Skills = model.Skills ?? "";
            vol.Availability = model.Availability;
            vol.Experience = model.Experience;
            vol.LanguagesSpoken = model.LanguagesSpoken;

            if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
                vol.ProfilePicture = await SaveFile(ProfilePictureFile, "profiles");

            vol.UpdatedAt = DateTime.UtcNow;
            vol.LastAction = "Profile Updated";

            _context.SaveChanges();
            LogActivity("Profile Updated", $"{vol.FullName} updated their profile", vol.Id, vol.FullName, "Volunteer");

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Dashboard", "Volunteer");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrganization(Organization model, IFormFile? LogoFile)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (role != "Organization" || userId == null)
                return RedirectToAction("Login", "Account");

            var org = _context.Organizations.Find(userId);
            if (org == null) return NotFound();

            org.OrganizationName = model.OrganizationName ?? "";
            org.OrganizationType = model.OrganizationType ?? "";
            org.OtherOrganizationType = model.OrganizationType == "Other" ? model.OtherOrganizationType : null;
            org.OrganizationDescription = model.OrganizationDescription ?? "";
            org.PhoneNumber = model.PhoneNumber ?? "";
            org.Address = model.Address ?? "";
            org.Latitude = model.Latitude;
            org.Longitude = model.Longitude;
            if (!string.IsNullOrWhiteSpace(model.City)) org.City = model.City;
            else if (!string.IsNullOrWhiteSpace(model.Address)) org.City = model.Address;
            if (!string.IsNullOrWhiteSpace(model.Division)) org.Division = model.Division;
            else if (!string.IsNullOrWhiteSpace(model.Address)) org.Division = model.Address;
            org.Website = model.Website;
            org.FacebookProfileLink = model.FacebookProfileLink;
            org.ContactPersonName = model.ContactPersonName ?? "";
            org.ContactPersonDesignation = model.ContactPersonDesignation ?? "";
            org.ContactPersonPhone = model.ContactPersonPhone ?? "";
            org.ContactPersonEmail = model.ContactPersonEmail ?? "";

            if (LogoFile != null && LogoFile.Length > 0)
                org.Logo = await SaveFile(LogoFile, "organizations");

            org.UpdatedAt = DateTime.UtcNow;
            org.LastAction = "Profile Updated";

            _context.SaveChanges();
            LogActivity("Profile Updated", $"{org.OrganizationName} updated their profile", org.Id, org.OrganizationName, "Organization");

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Dashboard", "Organization");
        }

      
        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(GetRole()))
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var role = GetRole();
            var userId = GetUserId();

            if (string.IsNullOrEmpty(role) || userId == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "All fields are required");
                TempData["ErrorMessage"] = "All fields are required";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match");
                TempData["ErrorMessage"] = "New passwords do not match";
                return View();
            }

            if (!PasswordHelper.IsStrongPassword(newPassword, out string passError))
            {
                ModelState.AddModelError("", passError);
                TempData["ErrorMessage"] = passError;
                return View();
            }

            string? currentHash = null;

            if (role == "Disability")
            {
                var u = _context.DisabilityUsers.Find(userId);
                if (u != null) currentHash = u.PasswordHash;
            }
            else if (role == "Volunteer")
            {
                var v = _context.Volunteers.Find(userId);
                if (v != null) currentHash = v.PasswordHash;
            }
            else if (role == "Organization")
            {
                var o = _context.Organizations.Find(userId);
                if (o != null) currentHash = o.PasswordHash;
            }
            else if (role == "Admin")
            {
                var a = _context.Admins.Find(userId);
                if (a != null) currentHash = a.PasswordHash;
            }

            if (currentHash == null || !PasswordHelper.VerifyPassword(currentPassword, currentHash))
            {
                ModelState.AddModelError("", "Current password is incorrect");
                TempData["ErrorMessage"] = "Current password is incorrect";
                return View();
            }

            var newHash = PasswordHelper.HashPassword(newPassword);

            if (role == "Disability")
            {
                var u = _context.DisabilityUsers.Find(userId);
                if (u != null)
                {
                    u.PasswordHash = newHash;
                    u.UpdatedAt = DateTime.UtcNow;
                    u.LastPasswordChangedAt = DateTime.UtcNow;
                    u.LastAction = "Password Changed";
                    LogActivity("Password Changed", $"{u.FullName} changed password", u.Id, u.FullName, "Disability");
                }
            }
            else if (role == "Volunteer")
            {
                var v = _context.Volunteers.Find(userId);
                if (v != null)
                {
                    v.PasswordHash = newHash;
                    v.UpdatedAt = DateTime.UtcNow;
                    v.LastPasswordChangedAt = DateTime.UtcNow;
                    v.LastAction = "Password Changed";
                    LogActivity("Password Changed", $"{v.FullName} changed password", v.Id, v.FullName, "Volunteer");
                }
            }
            else if (role == "Organization")
            {
                var o = _context.Organizations.Find(userId);
                if (o != null)
                {
                    o.PasswordHash = newHash;
                    o.UpdatedAt = DateTime.UtcNow;
                    o.LastPasswordChangedAt = DateTime.UtcNow;
                    o.LastAction = "Password Changed";
                    LogActivity("Password Changed", $"{o.OrganizationName} changed password", o.Id, o.OrganizationName, "Organization");
                }
            }
            else if (role == "Admin")
            {
                var a = _context.Admins.Find(userId);
                if (a != null)
                {
                    a.PasswordHash = newHash;
                    a.UpdatedAt = DateTime.UtcNow;
                    LogActivity("Password Changed", $"{a.FullName} changed password", a.Id, a.FullName, "Admin");
                }
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Dashboard", role);
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

        private void LogActivity(string actionType, string description, int? targetUserId, string? targetUserName, string? targetUserType)
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
    }
}