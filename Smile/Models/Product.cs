using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>商品與服務</summary>
    public class Product
    {
        public int Id { get; set; }

        public int StoreId { get; set; }
        public virtual Store Store { get; set; }

        [Required(ErrorMessage = "請輸入名稱"), StringLength(50)]
        [Display(Name = "名稱")]
        public string Name { get; set; }

        [Range(0, 100000, ErrorMessage = "價格需介於 0 到 100000")]
        [Display(Name = "價格（元）")]
        public int Price { get; set; }

        [StringLength(200)]
        [Display(Name = "說明")]
        public string Description { get; set; }

        [Display(Name = "供應中")]
        public bool IsAvailable { get; set; }
    }
}
