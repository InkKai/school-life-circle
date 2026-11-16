using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>店家分類</summary>
    public class StoreCategory
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "分類名稱")]
        public string Name { get; set; }

        public int SortOrder { get; set; }

        /// <summary>分類圖示（表情符號，僅裝飾用）</summary>
        [StringLength(8)]
        public string Icon { get; set; }

        /// <summary>分類代表色，例如 #F97316</summary>
        [StringLength(7)]
        public string Color { get; set; }

        public virtual ICollection<Store> Stores { get; set; }
    }
}
