using System;
using System.ComponentModel.DataAnnotations;

namespace SDP1.Models
{
    public class AdminActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public int? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }
        public string? TargetUserType { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}