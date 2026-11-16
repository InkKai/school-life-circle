using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>好友關係：用對方的會員編號加入，雙方各存一筆（A→B、B→A）</summary>
    public class Friendship
    {
        public int Id { get; set; }

        [Index("IX_Friendship_Pair", 1, IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Index("IX_Friendship_Pair", 2, IsUnique = true)]
        public int FriendId { get; set; }
        public virtual User Friend { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
