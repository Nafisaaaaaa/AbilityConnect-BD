using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    public class AccessibilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private readonly string[] _permittedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public AccessibilityController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        #region Session / Role Helpers

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetUserRole() => HttpContext.Session.GetString("UserRole");
        private bool IsDisabilityUser() => GetUserRole() == "Disability";
        private bool IsAdmin() => GetUserRole() == "Admin";

        #endregion


        public async Task<IActionResult> Index(
            string? search,
            string? placeType,
            string? city,
            string? district,
            bool? ramp,
            bool? elevator,
            bool? toilet,
            bool? parking,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 9;
            if (page < 1) page = 1;

            var query = _context.AccessibilityPlaces.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.PlaceName.ToLower().Contains(term) ||
                    p.Description.ToLower().Contains(term) ||
                    p.Address.ToLower().Contains(term) ||
                    p.City.ToLower().Contains(term) ||
                    p.District.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(placeType) && placeType != "All")
            {
                query = query.Where(p => p.PlaceType == placeType);
            }

            if (!string.IsNullOrWhiteSpace(city) && city != "All")
            {
                query = query.Where(p => p.City.ToLower() == city.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(district) && district != "All")
            {
                query = query.Where(p => p.District.ToLower() == district.Trim().ToLower());
            }

            if (ramp.HasValue && ramp.Value)
            {
                query = query.Where(p => p.WheelchairRamp);
            }

            if (elevator.HasValue && elevator.Value)
            {
                query = query.Where(p => p.Elevator);
            }

            if (toilet.HasValue && toilet.Value)
            {
                query = query.Where(p => p.AccessibleToilet);
            }

            if (parking.HasValue && parking.Value)
            {
                query = query.Where(p => p.AccessibleParking);
            }

            
            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "score_desc" : sortOrder;
            query = sortOrder switch
            {
                "score_asc" => query.OrderBy(p => p.AccessibilityScore),
                "name_asc" => query.OrderBy(p => p.PlaceName),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.AccessibilityScore)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var places = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AccessibilityListViewModel
            {
                Places = places,
                SearchKeyword = search,
                PlaceType = placeType,
                City = city,
                District = district,
                WheelchairRamp = ramp,
                Elevator = elevator,
                AccessibleToilet = toilet,
                AccessibleParking = parking,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                AvailableCities = await _context.AccessibilityPlaces.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync(),
                AvailableDistricts = await _context.AccessibilityPlaces.Select(p => p.District).Distinct().OrderBy(d => d).ToListAsync(),
                AvailableTypes = new List<string> { "Hospital", "Shopping Mall", "Public Park", "Transit Station", "Government Office", "School", "Restaurant", "Bank", "Other" }
            };

            return View(viewModel);
        }

        
        public async Task<IActionResult> Details(int id)
        {
            var place = await _context.AccessibilityPlaces.FindAsync(id);
            if (place == null) return NotFound();

            var viewModel = new AccessibilityPlaceDetailsViewModel
            {
                Place = place,
                IsDisabilityUser = IsDisabilityUser(),
                IsAdmin = IsAdmin(),
                CurrentUserId = GetUserId()
            };

            return View(viewModel);
        }
    }
}
