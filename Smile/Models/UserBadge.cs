using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>使用者已獲得的徽章（徽章的定義在 Infrastructure/Progress.cs）</summary>
    public class UserBadge
    {
        public int Id { get; set; }

        [Index("IX_UserBadge", 1, IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(30)]
        [Index("IX_UserBadge", 2, IsUnique = true)]
        public string BadgeKey { get; set; }

        public DateTime EarnedAt { get; set; }
    }
}
