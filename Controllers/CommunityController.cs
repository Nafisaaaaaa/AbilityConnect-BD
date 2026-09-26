using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;

namespace SDP1.Controllers
{
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CommunityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private (int? userId, string? role, string? name, string? avatar) GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            var name = HttpContext.Session.GetString("UserName");
            var avatar = HttpContext.Session.GetString("ProfilePicture");
            return (userId, role, name, avatar);
        }

        private void SetUserInfo()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
        }

 
        [HttpGet]
        public async Task<IActionResult> Index(string? sort, string? search, string? filter)
        {
            SetUserInfo();
            var (userId, userRole, userName, userAvatar) = GetCurrentUser();

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
               
                return RedirectToAction("Login", "Account");
            }

            var query = _context.CommunityPosts
                .Where(p => !p.IsDeleted);

            if (userRole != "Admin")
            {
                query = query.Where(p => !p.IsHidden);
            }

            if (!string.IsNullOrWhiteSpace(filter) && (filter == "my" || filter == "myposts"))
            {
                query = query.Where(p => p.UserId == userId.Value && p.UserRole == userRole);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Content.ToLower().Contains(s) || p.AuthorName.ToLower().Contains(s));
            }

            var postsList = await query
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted && (userRole == "Admin" || !c.IsHidden)))
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            if (sort == "popular")
            {
                postsList = postsList.OrderByDescending(p => p.Likes.Count).ThenByDescending(p => p.CreatedAt).ToList();
            }

            var postViewModels = postsList.Select(p => new CommunityPostItemViewModel
            {
                Id = p.Id,
                UserId = p.UserId,
                UserRole = p.UserRole,
                AuthorName = p.AuthorName,
                AuthorAvatar = p.AuthorAvatar,
                Content = p.Content,
                ImagePath = p.ImagePath,
                IsPinned = p.IsPinned,
                IsHidden = p.IsHidden,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                LikeCount = p.Likes.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId.Value && l.UserRole == userRole),
                CommentCount = p.Comments.Count,
                CanEditOrDelete = (p.UserId == userId.Value && p.UserRole == userRole) || userRole == "Admin",
                Comments = p.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CommunityCommentItemViewModel
                    {
                        Id = c.Id,
                        CommunityPostId = c.CommunityPostId,
                        UserId = c.UserId,
                        UserRole = c.UserRole,
                        AuthorName = c.AuthorName,
                        AuthorAvatar = c.AuthorAvatar,
                        Content = c.Content,
                        IsHidden = c.IsHidden,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        CanDelete = (c.UserId == userId.Value && c.UserRole == userRole) || userRole == "Admin"
                    }).ToList()
            }).ToList();

            var viewModel = new CommunityFeedViewModel
            {
                Posts = postViewModels,
                CurrentUserId = userId.Value,
                CurrentUserRole = userRole,
                CurrentUserName = userName ?? "User",
                CurrentUserAvatar = userAvatar,
                CanCreatePost = userRole == "Disability" || userRole == "Admin",
                SearchQuery = search,
                Filter = filter,
                TotalPostsCount = postViewModels.Count
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreatePostInputModel model)
        {
            var (userId, userRole, userName, userAvatar) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            if (userRole != "Disability" && userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Only persons with disabilities can create new community posts.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Post content cannot be empty (minimum 3 characters).";
                return RedirectToAction("Index");
            }

            string? savedImagePath = null;
            if (model.PostImage != null && model.PostImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var ext = Path.GetExtension(model.PostImage.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    TempData["ErrorMessage"] = "Invalid image file type. Only JPG, PNG, WEBP, and GIF are allowed.";
                    return RedirectToAction("Index");
                }

                if (model.PostImage.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Image file size exceeds the 5MB limit.";
                    return RedirectToAction("Index");
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "community");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.PostImage.CopyToAsync(fileStream);
                }

                savedImagePath = $"/uploads/community/{uniqueFileName}";
            }

            var postAvatar = !string.IsNullOrEmpty(userAvatar)
                ? userAvatar
                : (userRole == "Admin" ? "/images/admin-avatar.png" : "/images/default-avatar.png");

            var post = new CommunityPost
            {
                UserId = userId.Value,
                UserRole = userRole,
                AuthorName = userName ?? (userRole == "Admin" ? "Administrator" : "Anonymous"),
                AuthorAvatar = postAvatar,
                Content = model.Content.Trim(),
                ImagePath = savedImagePath,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunityPosts.Add(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your post has been shared with the community!";
            return RedirectToAction("Index");
        }

       
        [HttpGet]
        public async Task<IActionResult> EditPost(int id)
        {
            SetUserInfo();
            var (userId, userRole, _, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Account");

            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (post == null) return NotFound();

            if (post.UserId != userId.Value || post.UserRole != userRole)
            {
                if (userRole != "Admin")
                {
                    TempData["ErrorMessage"] = "You can only edit your own posts.";
                    return RedirectToAction("Index");
                }
            }

            var model = new EditPostInputModel
            {
                Id = post.Id,
                Content = post.Content,
                ExistingImagePath = post.ImagePath
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(EditPostInputModel model)
        {
            var (userId, userRole, _, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Account");

            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);
            if (post == null) return NotFound();

            if (post.UserId != userId.Value || post.UserRole != userRole)
            {
                if (userRole != "Admin")
                {
                    TempData["ErrorMessage"] = "You can only edit your own posts.";
                    return RedirectToAction("Index");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            post.Content = model.Content.Trim();
            post.UpdatedAt = DateTime.UtcNow;

            if (model.RemoveExistingImage && !string.IsNullOrEmpty(post.ImagePath))
            {
                post.ImagePath = null;
            }

            if (model.NewImage != null && model.NewImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var ext = Path.GetExtension(model.NewImage.FileName).ToLowerInvariant();

                if (allowedExtensions.Contains(ext) && model.NewImage.Length <= 5 * 1024 * 1024)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "community");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.NewImage.CopyToAsync(fileStream);
                    }

                    post.ImagePath = $"/uploads/community/{uniqueFileName}";
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Post updated successfully.";
            return RedirectToAction("Index");
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var (userId, userRole, _, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Account");

            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

         
            if (!((post.UserId == userId.Value && post.UserRole == userRole) || userRole == "Admin"))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this post.";
                return RedirectToAction("Index");
            }

            post.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Post deleted successfully.";
            return RedirectToAction("Index");
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var (userId, userRole, _, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                return Json(new { success = false, message = "Please login to like posts." });
            }

            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var existingLike = await _context.CommunityPostLikes
                .FirstOrDefaultAsync(l => l.CommunityPostId == postId && l.UserId == userId.Value && l.UserRole == userRole);

            bool isLiked;
            if (existingLike != null)
            {
                _context.CommunityPostLikes.Remove(existingLike);
                isLiked = false;
            }
            else
            {
                _context.CommunityPostLikes.Add(new CommunityPostLike
                {
                    CommunityPostId = postId,
                    UserId = userId.Value,
                    UserRole = userRole,
                    CreatedAt = DateTime.UtcNow
                });
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            var totalLikes = await _context.CommunityPostLikes.CountAsync(l => l.CommunityPostId == postId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, isLiked, likeCount = totalLikes });
            }

            return RedirectToAction("Index");
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentInputModel model)
        {
            var (userId, userRole, userName, userAvatar) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Content))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction("Index");
            }

            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == model.PostId && !p.IsDeleted);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Post not found.";
                return RedirectToAction("Index");
            }

            var commentAvatar = !string.IsNullOrEmpty(userAvatar)
                ? userAvatar
                : (userRole == "Admin" ? "/images/admin-avatar.png" : "/images/default-avatar.png");

            var comment = new CommunityComment
            {
                CommunityPostId = model.PostId,
                UserId = userId.Value,
                UserRole = userRole,
                AuthorName = userName ?? (userRole == "Admin" ? "Administrator" : "User"),
                AuthorAvatar = commentAvatar,
                Content = model.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunityComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added.";
            return Redirect($"/Community#post-{model.PostId}");
        }

        
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            SetUserInfo();
            var (userId, userRole, userName, userAvatar) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Account");

            var post = await _context.CommunityPosts
                .Include(p => p.Likes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted && (userRole == "Admin" || !c.IsHidden)))
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (post == null) return NotFound();
            if (post.IsHidden && userRole != "Admin") return NotFound();

            var vm = new CommunityPostItemViewModel
            {
                Id = post.Id,
                UserId = post.UserId,
                UserRole = post.UserRole,
                AuthorName = post.AuthorName,
                AuthorAvatar = post.AuthorAvatar,
                Content = post.Content,
                ImagePath = post.ImagePath,
                IsPinned = post.IsPinned,
                IsHidden = post.IsHidden,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                LikeCount = post.Likes.Count,
                IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == userId.Value && l.UserRole == userRole),
                CommentCount = post.Comments.Count,
                CanEditOrDelete = (post.UserId == userId.Value && post.UserRole == userRole) || userRole == "Admin",
                Comments = post.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CommunityCommentItemViewModel
                    {
                        Id = c.Id,
                        CommunityPostId = c.CommunityPostId,
                        UserId = c.UserId,
                        UserRole = c.UserRole,
                        AuthorName = c.AuthorName,
                        AuthorAvatar = c.AuthorAvatar,
                        Content = c.Content,
                        IsHidden = c.IsHidden,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        CanDelete = (c.UserId == userId.Value && c.UserRole == userRole) || userRole == "Admin"
                    }).ToList()
            };

            ViewBag.CurrentUserId = userId.Value;
            ViewBag.CurrentUserRole = userRole;
            ViewBag.CurrentUserName = userName ?? "User";
            ViewBag.CurrentUserAvatar = userAvatar;

            return View(vm);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var (userId, userRole, _, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Account");

            var comment = await _context.CommunityComments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            if (!((comment.UserId == userId.Value && comment.UserRole == userRole) || userRole == "Admin"))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this comment.";
                return RedirectToAction("Index");
            }

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment deleted.";
            return Redirect($"/Community#post-{comment.CommunityPostId}");
        }

      
        [HttpGet]
        public async Task<IActionResult> Report(string? type, int? postId, int? commentId, string? placeName, string? address, double? lat, double? lng)
        {
            SetUserInfo();
            var (userId, userRole, _, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            var reportType = string.Equals(type, "Post", StringComparison.OrdinalIgnoreCase) ? "Post" :
                             string.Equals(type, "Comment", StringComparison.OrdinalIgnoreCase) ? "Comment" : "Location";

            var model = new CommunityReportCreateViewModel
            {
                ReportType = reportType,
                PostId = postId,
                CommentId = commentId,
                PlaceName = placeName,
                Address = address,
                Latitude = lat ?? 23.8103,
                Longitude = lng ?? 90.4125,
                Reason = reportType == "Location" ? "Difficult Entrance" : "Spam"
            };

            if (postId.HasValue)
            {
                var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId.Value);
                if (post != null)
                {
                    model.AvailablePosts.Add(post);
                }
            }

            if (commentId.HasValue)
            {
                var comment = await _context.CommunityComments.FirstOrDefaultAsync(c => c.Id == commentId.Value);
                if (comment != null)
                {
                    model.AvailableComments.Add(comment);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportContent(ReportModalInputModel model)
        {
            var (userId, userRole, userName, _) = GetCurrentUser();
            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            var reportType = string.IsNullOrWhiteSpace(model.ReportType) ?
                (model.CommentId.HasValue ? "Comment" : model.PostId.HasValue ? "Post" : "Location") :
                model.ReportType;

            if (reportType == "Location")
            {
                if (string.IsNullOrWhiteSpace(model.PlaceName) && string.IsNullOrWhiteSpace(model.Address))
                {
                    TempData["ErrorMessage"] = "Please specify a location name or address for the report.";
                    return RedirectToAction("Report", new { type = "Location" });
                }

                var locationReport = new CommunityReport
                {
                    ReporterUserId = userId.Value,
                    ReporterUserRole = userRole,
                    ReporterName = userName ?? "User",
                    ReportType = "Location",
                    PlaceName = model.PlaceName?.Trim(),
                    Address = model.Address?.Trim(),
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Reason = string.IsNullOrWhiteSpace(model.Reason) ? "Difficult Entrance" : model.Reason,
                    Details = model.Details?.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.CommunityReports.Add(locationReport);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you. Your location accessibility report has been submitted to the community moderation team.";
                return RedirectToAction("Index", "Accessibility");
            }
            else
            {
                if (!model.PostId.HasValue && !model.CommentId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid report target.";
                    return RedirectToAction("Index");
                }

                
                var alreadyReported = await _context.CommunityReports.AnyAsync(r =>
                    r.ReporterUserId == userId.Value &&
                    r.ReporterUserRole == userRole &&
                    r.PostId == model.PostId &&
                    r.CommentId == model.CommentId);

                if (alreadyReported)
                {
                    TempData["ErrorMessage"] = "You have already submitted a report for this content. Our moderation team is reviewing it.";
                    return RedirectToAction("Index");
                }

                var report = new CommunityReport
                {
                    ReporterUserId = userId.Value,
                    ReporterUserRole = userRole,
                    ReporterName = userName ?? "User",
                    ReportType = model.CommentId.HasValue ? "Comment" : "Post",
                    PostId = model.PostId,
                    CommentId = model.CommentId,
                    Reason = model.Reason,
                    Details = model.Details?.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.CommunityReports.Add(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you. Your report has been submitted for moderation review.";
                return RedirectToAction("Index");
            }
        }

       
        [HttpGet]
        public async Task<IActionResult> Moderation(string? tab)
        {
            SetUserInfo();
            var (_, userRole, _, _) = GetCurrentUser();

            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Access restricted to administrators.";
                return RedirectToAction("Index");
            }

            var reports = await _context.CommunityReports
                .Include(r => r.Post)
                .Include(r => r.Comment)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var posts = await _context.CommunityPosts
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var comments = await _context.CommunityComments
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var model = new CommunityModerationViewModel
            {
                AllReports = reports,
                AllPosts = posts,
                AllComments = comments,
                ActiveTab = string.IsNullOrEmpty(tab) ? "posts" : tab.ToLower(),
                PendingReportsCount = reports.Count(r => r.Status == "Pending"),
                TotalPostsCount = posts.Count,
                TotalCommentsCount = comments.Count
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHidePost(int id)
        {
            var (_, userRole, _, _) = GetCurrentUser();
            if (userRole != "Admin") return Forbid();

            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            post.IsHidden = !post.IsHidden;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = post.IsHidden ? "Post hidden from community feed." : "Post unhidden and visible to community.";
            return RedirectToAction("Moderation", new { tab = "posts" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHideComment(int id)
        {
            var (_, userRole, _, _) = GetCurrentUser();
            if (userRole != "Admin") return Forbid();

            var comment = await _context.CommunityComments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            comment.IsHidden = !comment.IsHidden;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = comment.IsHidden ? "Comment hidden from community view." : "Comment restored and visible.";
            return RedirectToAction("Moderation", new { tab = "comments" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReportStatus(int id, string status, string? adminNotes)
        {
            var (_, userRole, _, _) = GetCurrentUser();
            if (userRole != "Admin") return Forbid();

            var report = await _context.CommunityReports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            report.Status = status;
            report.AdminNotes = adminNotes?.Trim();
            report.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Report #{id} marked as {status}.";
            return RedirectToAction("Moderation", new { tab = "reports" });
        }
    }
}
