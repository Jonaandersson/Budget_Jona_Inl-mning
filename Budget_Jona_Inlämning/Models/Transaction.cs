using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Budget_Jona_Inlämning.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public bool IsMonthly { get; set; }

        public bool IsIncome { get; set; }

        public int? CategoryId { get; set; }

        public Category? Category { get; set; }
    }
}
