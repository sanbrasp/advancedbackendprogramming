using Shop.Core.DTOs;
using Shop.Core.Interfaces;

namespace Shop.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.CategoryId));
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var result = await _productRepository.GetByIdAsync(id);
        
        return result == null ? null : new ProductDto(result.Id, result.Name, result.Price, result.CategoryId);
    }
}