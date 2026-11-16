using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>使用者領到的優惠券（我的優惠券）</summary>
    public class UserCoupon
    {
        public int Id { get; set; }

        public int CouponId { get; set; }
        public virtual Coupon Coupon { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(20)]
        [Index(IsUnique = true)]
        [Display(Name = "核銷碼")]
        public string Code { get; set; }

        [StringLength(40)]
        [Index]
        public string ShareToken { get; set; }

        /// <summary>這張券是哪位好友送的（自己領的為 null）</summary>
        public int? GiftedFromId { get; set; }
        public virtual User GiftedFrom { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; }

        public DateTime ClaimedAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public virtual Redemption Redemption { get; set; }
    }
}
