using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>每日簽到：每人每天一筆，簽到時順便抽一次扭蛋（獎品只有經驗值和徽章，跟金錢無關）</summary>
    public class CheckIn
    {
        public int Id { get; set; }

        [Index("IX_CheckIn_UserDay", 1, IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        /// <summary>簽到日期（只取日期部分）</summary>
        [Index("IX_CheckIn_UserDay", 2, IsUnique = true)]
        [Column(TypeName = "date")]
        public DateTime Day { get; set; }

        /// <summary>抽到的獎項代碼，見 Infrastructure/Gacha.cs</summary>
        [Required, StringLength(20)]
        public string Prize { get; set; }

        /// <summary>這次拿到的經驗值（含連續簽到加成）</summary>
        public int Xp { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
