using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>學校（找店的基準點）</summary>
    public class School
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "學校名稱")]
        public string Name { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "簡稱")]
        public string ShortName { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
