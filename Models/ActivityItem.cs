using System;

namespace SDP1.Models
{
    public class ActivityItem
    {
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}