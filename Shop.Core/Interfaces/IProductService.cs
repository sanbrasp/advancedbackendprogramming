using Shop.Core.DTOs;

namespace Shop.Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    
    Task<ProductDto?> GetProductAsync(int id);
}