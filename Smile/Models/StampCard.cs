using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>集點卡：每位會員在每間店各一張</summary>
    public class StampCard
    {
        public int Id { get; set; }

        [Index("IX_StampCard_UserStore", 1, IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Index("IX_StampCard_UserStore", 2, IsUnique = true)]
        public int StoreId { get; set; }
        public virtual Store Store { get; set; }

        [Display(Name = "目前點數")]
        public int Stamps { get; set; }

        [Display(Name = "已兌換次數")]
        public int RewardsRedeemed { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<StampRecord> Records { get; set; }
    }
}
