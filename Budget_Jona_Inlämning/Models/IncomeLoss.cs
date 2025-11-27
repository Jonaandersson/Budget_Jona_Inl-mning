using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Budget_Jona_Inlämning.Models
{
    public class IncomeLoss
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        // Total income lost (e.g., one day's salary)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountLost { get; set; }

        // Number of days lost
        public int LostDays { get; set; }
        // Percentage refunded (e.g., 0.8 = 80%)
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal RefundPercentage { get; set; } = 0.80m;

        [NotMapped]
        public decimal RefundAmount => this.AmountLost * this.RefundPercentage;

        public string Description { get; set; } = string.Empty;
    }
}
