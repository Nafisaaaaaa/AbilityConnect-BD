using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDP1.Models
{
    public class SavedJob
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }

        [Required]
        public int DisabilityUserId { get; set; }

        [ForeignKey("DisabilityUserId")]
        public virtual DisabilityUser? DisabilityUser { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
