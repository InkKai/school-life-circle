using System;
using System.ComponentModel.DataAnnotations;

namespace Smile.Models
{
    /// <summary>集點紀錄：蓋章（+N）或兌換獎勵（-集滿點數）</summary>
    public class StampRecord
    {
        public int Id { get; set; }

        public int StampCardId { get; set; }
        public virtual StampCard StampCard { get; set; }

        /// <summary>Stamp 或 Reward，見 StampKinds</summary>
        [Required, StringLength(10)]
        public string Kind { get; set; }

        /// <summary>點數變化，蓋章為正、兌換為負</summary>
        public int Change { get; set; }

        /// <summary>操作的店員（店家帳號）</summary>
        public int OperatorId { get; set; }
        public virtual User Operator { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
