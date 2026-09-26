using Microsoft.AspNetCore.Mvc;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDP1.Controllers
{
    public class LearningHubController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LearningHubController(ApplicationDbContext context)
        {
            _context = context;
        }

        private void SetUserInfo()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
        }

        public IActionResult Index(string? category, string? search, int page = 1)
        {
            SetUserInfo();
            var pageSize = 9;

            var query = _context.LearningVideos
                .Where(v => v.IsPublished);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(v => v.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(v => v.Title.ToLower().Contains(s) || v.Description.ToLower().Contains(s));
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var rawVideos = query
                .OrderByDescending(v => v.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var videoCards = rawVideos.Select(v => new LearningVideoCardViewModel
            {
                Id = v.Id,
                Title = v.Title,
                Category = v.Category,
                Description = v.Description,
                YouTubeUrl = v.YouTubeUrl,
                VideoId = YouTubeHelper.ExtractVideoId(v.YouTubeUrl),
                EmbedUrl = YouTubeHelper.GetEmbedUrl(v.YouTubeUrl),
                ThumbnailUrl = YouTubeHelper.GetThumbnailUrl(v.YouTubeUrl),
                Duration = v.Duration,
                IsPublished = v.IsPublished,
                CreatedAt = v.CreatedAt
            }).ToList();

            var counts = _context.LearningVideos
                .Where(v => v.IsPublished)
                .GroupBy(v => v.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            var viewModel = new LearningHubIndexViewModel
            {
                Videos = videoCards,
                SelectedCategory = category,
                SearchQuery = search,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                CategoryCounts = counts
            };

            return View(viewModel);
        }

        public IActionResult Watch(int id)
        {
            SetUserInfo();
            var userRole = HttpContext.Session.GetString("UserRole");

            var video = _context.LearningVideos.FirstOrDefault(v => v.Id == id);
            if (video == null) return NotFound();

            if (!video.IsPublished && userRole != "Admin")
            {
                return NotFound();
            }

            var videoId = YouTubeHelper.ExtractVideoId(video.YouTubeUrl);
            var embedUrl = YouTubeHelper.GetEmbedUrl(video.YouTubeUrl);

            var related = _context.LearningVideos
                .Where(v => v.Category == video.Category && v.Id != video.Id && v.IsPublished)
                .OrderByDescending(v => v.CreatedAt)
                .Take(4)
                .Select(v => new LearningVideoCardViewModel
                {
                    Id = v.Id,
                    Title = v.Title,
                    Category = v.Category,
                    Duration = v.Duration,
                    YouTubeUrl = v.YouTubeUrl,
                    VideoId = YouTubeHelper.ExtractVideoId(v.YouTubeUrl),
                    ThumbnailUrl = YouTubeHelper.GetThumbnailUrl(v.YouTubeUrl)
                })
                .ToList();

            var viewModel = new LearningVideoDetailsViewModel
            {
                Video = video,
                VideoId = videoId,
                EmbedUrl = embedUrl,
                RelatedVideos = related
            };

            return View(viewModel);
        }
    }
}
