using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>優惠券（店家發行的券種）</summary>
    public class Coupon
    {
        public int Id { get; set; }

        public int StoreId { get; set; }
        public virtual Store Store { get; set; }

        [Required(ErrorMessage = "請輸入標題"), StringLength(50)]
        [Display(Name = "優惠標題")]
        public string Title { get; set; }

        [StringLength(300)]
        [Display(Name = "使用說明")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "開始日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        [Display(Name = "結束日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime EndDate { get; set; }

        [Range(1, 10000, ErrorMessage = "張數需介於 1 到 10000")]
        [Display(Name = "發行張數")]
        public int TotalQuantity { get; set; }

        [Display(Name = "已領取")]
        public int IssuedCount { get; set; }

        [Display(Name = "上架中")]
        public bool IsActive { get; set; }

        /// <summary>參加的活動（沒有參加為 null）</summary>
        [Display(Name = "參加活動")]
        public int? CampaignId { get; set; }
        public virtual Campaign Campaign { get; set; }

        public virtual ICollection<UserCoupon> UserCoupons { get; set; }

        [NotMapped]
        public int Remaining { get { return Math.Max(0, TotalQuantity - IssuedCount); } }

        public bool IsClaimable(DateTime today)
        {
            return IsActive && today >= StartDate.Date && today <= EndDate.Date && IssuedCount < TotalQuantity;
        }
    }
}
