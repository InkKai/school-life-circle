using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>店家</summary>
    public class Store
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入店名"), StringLength(50)]
        [Display(Name = "店名")]
        public string Name { get; set; }

        [StringLength(500)]
        [Display(Name = "店家介紹")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "請輸入地址"), StringLength(100)]
        [Display(Name = "地址")]
        public string Address { get; set; }

        [StringLength(20)]
        [Display(Name = "電話")]
        public string Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "營業時間")]
        public string OpenHours { get; set; }

        [Display(Name = "緯度")]
        [Range(-90, 90, ErrorMessage = "緯度需介於 -90 到 90")]
        public double Latitude { get; set; }

        [Display(Name = "經度")]
        [Range(-180, 180, ErrorMessage = "經度需介於 -180 到 180")]
        public double Longitude { get; set; }

        [StringLength(100)]
        [Display(Name = "可用支付方式")]
        public string PaymentMethods { get; set; }

        [Display(Name = "入口無階梯")]
        public bool StepFreeEntrance { get; set; }

        [Display(Name = "有斜坡道")]
        public bool HasRamp { get; set; }

        [Display(Name = "有無障礙廁所")]
        public bool AccessibleToilet { get; set; }

        [Display(Name = "無障礙資訊已實地查證")]
        public bool AccessibilityVerified { get; set; }

        [Display(Name = "無障礙資訊已知")]
        public bool AccessibilityKnown { get; set; }

        /// <summary>資料來源：Merchant（店家申請）、OSM（OpenStreetMap）、Demo（示範）</summary>
        [StringLength(20)]
        public string DataSource { get; set; }

        [StringLength(20)]
        public string OsmId { get; set; }

        [StringLength(50)]
        [Display(Name = "料理類型")]
        public string Cuisine { get; set; }

        /// <summary>集點卡：集滿幾點可兌換，0 表示未開啟</summary>
        [Range(0, 30, ErrorMessage = "集點數需介於 0 到 30")]
        [Display(Name = "集滿幾點可兌換")]
        public int StampGoal { get; set; }

        [StringLength(50)]
        [Display(Name = "集滿可兌換")]
        public string StampReward { get; set; }

        /// <summary>店家自己上傳的照片（店面、老闆、招牌菜），存在 ~/Uploads/stores</summary>
        [StringLength(200)]
        [Display(Name = "店家照片")]
        public string PhotoUrl { get; set; }

        /// <summary>精選店家：出現在首頁「精選店家」，由平台管理者設定（加值方案的曝光位置）</summary>
        [Display(Name = "精選店家")]
        public bool IsFeatured { get; set; }

        [StringLength(120)]
        [Display(Name = "老闆的話")]
        public string OwnerMessage { get; set; }

        [Display(Name = "示範資料")]
        public bool IsDemo { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "狀態")]
        public string Status { get; set; }

        [Display(Name = "分類")]
        public int CategoryId { get; set; }
        public virtual StoreCategory Category { get; set; }

        public int OwnerId { get; set; }
        public virtual User Owner { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Coupon> Coupons { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}
