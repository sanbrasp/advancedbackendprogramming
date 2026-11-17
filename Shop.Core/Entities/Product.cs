using System.ComponentModel.DataAnnotations;

namespace Shop.Core.Entities;

public class Product
{
    public int Id { get; set; }
    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}