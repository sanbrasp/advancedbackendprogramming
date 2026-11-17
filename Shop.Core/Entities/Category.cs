using System.ComponentModel.DataAnnotations;

namespace Shop.Core.Entities;

public class Category
{
    public int Id { get; set; }
    [Required] [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new List<Product>();
}