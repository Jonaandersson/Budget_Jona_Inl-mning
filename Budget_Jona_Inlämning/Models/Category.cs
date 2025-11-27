#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Budget_Jona_Inlämning.Models;

public enum CategoryType
{
    Expense = 0,
    Income = 1
}

public class Category
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; } = CategoryType.Expense;

    public List<Transaction> Transactions { get; set; } = new();
}