using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace SDP1.Models
{
 
    public class CommunityPost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(30)]
        public string UserRole { get; set; } = string.Empty; 

        [Required]
        [StringLength(120)]
        public string AuthorName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? AuthorAvatar { get; set; }

        [Required(ErrorMessage = "Post content cannot be empty")]
        [StringLength(3000, MinimumLength = 3, ErrorMessage = "Post must be between 3 and 3000 characters")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImagePath { get; set; }

        public bool IsPinned { get; set; } = false;

        public bool IsHidden { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        
        public virtual ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
        public virtual ICollection<CommunityPostLike> Likes { get; set; } = new List<CommunityPostLike>();
        public virtual ICollection<CommunityReport> Reports { get; set; } = new List<CommunityReport>();
    }

    public class CommunityComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommunityPostId { get; set; }

        [ForeignKey("CommunityPostId")]
        public virtual CommunityPost? Post { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(30)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string AuthorName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? AuthorAvatar { get; set; }

        [Required(ErrorMessage = "Comment cannot be empty")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public bool IsHidden { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<CommunityReport> Reports { get; set; } = new List<CommunityReport>();
    }

    public class CommunityPostLike
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommunityPostId { get; set; }

        [ForeignKey("CommunityPostId")]
        public virtual CommunityPost? Post { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(30)]
        public string UserRole { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CommunityReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReporterUserId { get; set; }

        [Required]
        [StringLength(30)]
        public string ReporterUserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string ReporterName { get; set; } = string.Empty;

        public int? PostId { get; set; }

        [ForeignKey("PostId")]
        public virtual CommunityPost? Post { get; set; }

        public int? CommentId { get; set; }

        [ForeignKey("CommentId")]
        public virtual CommunityComment? Comment { get; set; }

        [StringLength(30)]
        public string ReportType { get; set; } = "Post"; 

        [StringLength(200)]
        public string? PlaceName { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Please choose a reason for reporting")]
        [StringLength(60)]
        public string Reason { get; set; } = "Spam"; 

        [StringLength(600)]
        public string? Details { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; 

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }
    }

    

    public class CommunityFeedViewModel
    {
        public List<CommunityPostItemViewModel> Posts { get; set; } = new();
        public int CurrentUserId { get; set; }
        public string CurrentUserRole { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string? CurrentUserAvatar { get; set; }
        public bool CanCreatePost { get; set; }
        public string? SearchQuery { get; set; }
        public string? Filter { get; set; }
        public int TotalPostsCount { get; set; }
    }

    public class CommunityPostItemViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool IsPinned { get; set; }
        public bool IsHidden { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public int CommentCount { get; set; }
        public bool CanEditOrDelete { get; set; }

        public List<CommunityCommentItemViewModel> Comments { get; set; } = new();
    }

    public class CommunityCommentItemViewModel
    {
        public int Id { get; set; }
        public int CommunityPostId { get; set; }
        public int UserId { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreatePostInputModel
    {
        [Required(ErrorMessage = "Please write something to share.")]
        [StringLength(3000, MinimumLength = 3, ErrorMessage = "Post must be between 3 and 3000 characters.")]
        public string Content { get; set; } = string.Empty;

        public IFormFile? PostImage { get; set; }
    }

    public class EditPostInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Content cannot be empty.")]
        [StringLength(3000, MinimumLength = 3, ErrorMessage = "Post must be between 3 and 3000 characters.")]
        public string Content { get; set; } = string.Empty;

        public string? ExistingImagePath { get; set; }
        public bool RemoveExistingImage { get; set; }
        public IFormFile? NewImage { get; set; }
    }

    public class AddCommentInputModel
    {
        [Required]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Comment cannot be empty.")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 1000 characters.")]
        public string Content { get; set; } = string.Empty;
    }

    public class ReportModalInputModel
    {
        public string ReportType { get; set; } = "Post"; 
        public int? PostId { get; set; }
        public int? CommentId { get; set; }

        public string? PlaceName { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Please choose a reason for reporting.")]
        public string Reason { get; set; } = "Spam";

        [StringLength(600)]
        public string? Details { get; set; }
    }

    public class CommunityReportCreateViewModel
    {
        public string ReportType { get; set; } = "Location"; 
        public int? PostId { get; set; }
        public int? CommentId { get; set; }

        public string? PlaceName { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string Reason { get; set; } = "Difficult Entrance";
        public string? Details { get; set; }

        public List<CommunityPost> AvailablePosts { get; set; } = new();
        public List<CommunityComment> AvailableComments { get; set; } = new();
    }

    public class AdminCommunityReportsViewModel
    {
        public List<CommunityReport> Reports { get; set; } = new();
        public string CurrentType { get; set; } = "All";
        public string CurrentStatus { get; set; } = "All"; 
        public string? SearchQuery { get; set; }

        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ReviewedCount { get; set; }
        public int ResolvedCount { get; set; }
        public int PostReportsCount { get; set; }
        public int CommentReportsCount { get; set; }
        public int LocationReportsCount { get; set; }
    }

    public class CommunityModerationViewModel
    {
        public List<CommunityPost> AllPosts { get; set; } = new();
        public List<CommunityComment> AllComments { get; set; } = new();
        public List<CommunityReport> AllReports { get; set; } = new();
        public string ActiveTab { get; set; } = "reports"; 
        public int PendingReportsCount { get; set; }
        public int TotalPostsCount { get; set; }
        public int TotalCommentsCount { get; set; }
    }
}
