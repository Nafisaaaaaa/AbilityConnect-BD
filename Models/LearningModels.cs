using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SDP1.Models
{
    public class LearningVideo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Video title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Lesson Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50)]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "YouTube URL is required")]
        [StringLength(500)]
        [Display(Name = "YouTube URL")]
        public string YouTubeUrl { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Duration (e.g., 15 mins)")]
        public string? Duration { get; set; }

        [Display(Name = "Published")]
        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public static class LearningCategories
    {
        public const string Braille = "Braille";
        public const string SignLanguage = "Sign Language";
        public const string ComputerSkills = "Computer Skills";
        public const string Freelancing = "Freelancing";
        public const string DigitalLiteracy = "Digital Literacy";

        public static readonly string[] All = new[]
        {
            Braille,
            SignLanguage,
            ComputerSkills,
            Freelancing,
            DigitalLiteracy
        };
    }

    public static class YouTubeHelper
    {
        private static readonly Regex YouTubeRegex = new Regex(
            @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string? ExtractVideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = YouTubeRegex.Match(url);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        public static bool IsValidYouTubeUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(ExtractVideoId(url));
        }

        public static string? GetEmbedUrl(string? url)
        {
            var videoId = ExtractVideoId(url);
            if (string.IsNullOrEmpty(videoId))
                return null;

            return $"https://www.youtube-nocookie.com/embed/{videoId}";
        }

        public static string? GetThumbnailUrl(string? url)
        {
            var videoId = ExtractVideoId(url);
            if (string.IsNullOrEmpty(videoId))
                return null;

            return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
        }
    }

    public class LearningHubIndexViewModel
    {
        public List<LearningVideoCardViewModel> Videos { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 9;
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
    }

    public class LearningVideoCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string YouTubeUrl { get; set; } = string.Empty;
        public string? VideoId { get; set; }
        public string? EmbedUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Duration { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LearningVideoDetailsViewModel
    {
        public LearningVideo Video { get; set; } = null!;
        public string? VideoId { get; set; }
        public string? EmbedUrl { get; set; }
        public List<LearningVideoCardViewModel> RelatedVideos { get; set; } = new();
    }

    public class LearningVideoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Lesson title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Lesson Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "YouTube URL is required")]
        [StringLength(500)]
        [Display(Name = "YouTube URL")]
        public string YouTubeUrl { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Duration (e.g. 15 mins, 1 hour)")]
        public string? Duration { get; set; }

        [Display(Name = "Publish publicly")]
        public bool IsPublished { get; set; } = true;
    }
}
