using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smile.Models
{
    /// <summary>使用者</summary>
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100), EmailAddress]
        [Index(IsUnique = true)]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        [Required, StringLength(200)]
        public string PasswordHash { get; set; }

        [Required, StringLength(100)]
        public string PasswordSalt { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "顯示名稱")]
        public string DisplayName { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "角色")]
        public string Role { get; set; }

        [Display(Name = "學校")]
        public int? SchoolId { get; set; }
        public virtual School School { get; set; }

        [Display(Name = "預設使用大字模式")]
        public bool PreferLargeText { get; set; }

        /// <summary>會員編號（10 位數字），店家掃描會員卡 QR Code 或輸入此號碼來集點</summary>
        [Required, StringLength(10)]
        [Index(IsUnique = true)]
        [Display(Name = "會員編號")]
        public string MemberCode { get; set; }

        /// <summary>財政部電子發票手機條碼載具，例如 /ABC1234。平台只負責顯示，不代開發票</summary>
        [StringLength(8)]
        [Display(Name = "手機條碼載具")]
        public string CarrierCode { get; set; }

        /// <summary>邀請碼（8 碼），放在邀請連結裡；跟集點用的會員編號分開，避免把會員編號公開</summary>
        [Required, StringLength(8)]
        [Index(IsUnique = true)]
        public string InviteCode { get; set; }

        /// <summary>是哪位會員邀請來的（自己來的為 null）</summary>
        public int? ReferredById { get; set; }
        public virtual User ReferredBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<UserCoupon> UserCoupons { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
    }
}
