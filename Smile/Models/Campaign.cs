using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>活動：配合學校行事曆的主題優惠檔期（考試週、社團成果展…），店家可以讓優惠券參加</summary>
    public class Campaign
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入活動名稱"), StringLength(30)]
        [Display(Name = "活動名稱")]
        public string Title { get; set; }

        [StringLength(60)]
        [Display(Name = "一句話介紹")]
        public string Tagline { get; set; }

        [Display(Name = "開始日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        [Display(Name = "結束日期")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime EndDate { get; set; }

        /// <summary>主題色，見 CampaignThemes</summary>
        [Required, StringLength(10)]
        [Display(Name = "主題色")]
        public string Theme { get; set; }

        [Display(Name = "上架中")]
        public bool IsActive { get; set; }

        public virtual ICollection<Coupon> Coupons { get; set; }

        public bool IsRunning(DateTime today)
        {
            return IsActive && StartDate.Date <= today && EndDate.Date >= today;
        }

        public bool IsUpcoming(DateTime today)
        {
            return IsActive && StartDate.Date > today;
        }
    }
}
