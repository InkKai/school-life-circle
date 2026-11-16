using System;
using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>評論</summary>
    public class Review
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int StoreId { get; set; }
        public virtual Store Store { get; set; }

        [Range(1, 5, ErrorMessage = "請給 1 到 5 顆星")]
        [Display(Name = "評分")]
        public int Rating { get; set; }

        [StringLength(300)]
        [Display(Name = "評論")]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
