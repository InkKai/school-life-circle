using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>核銷紀錄（每張優惠券最多一筆）</summary>
    public class Redemption
    {
        [Key, ForeignKey("UserCoupon")]
        public int UserCouponId { get; set; }
        public virtual UserCoupon UserCoupon { get; set; }

        public int StoreId { get; set; }
        public virtual Store Store { get; set; }

        public int RedeemedById { get; set; }
        public virtual User RedeemedBy { get; set; }

        public DateTime RedeemedAt { get; set; }
    }
}
