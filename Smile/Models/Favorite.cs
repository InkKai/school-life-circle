using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>收藏</summary>
    public class Favorite
    {
        public int Id { get; set; }

        [Index("IX_UserStore", 1, IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Index("IX_UserStore", 2, IsUnique = true)]
        public int StoreId { get; set; }
        public virtual Store Store { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
