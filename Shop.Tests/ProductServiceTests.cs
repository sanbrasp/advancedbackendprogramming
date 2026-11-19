using Moq;
using Shop.Core.Entities;
using Shop.Core.Interfaces;
using Shop.Core.Services;

namespace Shop.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsThreeDtos_WhenRepositoryReturnsThreeProducts()
    {
        // Arrange (fake repo, moq builds a class that implements IProductRepository)
        var mockRepo = new Mock<IProductRepository>();
        // When the service calls GetAllAsync, the fake returns three products. No DB involved
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>
            {
                new Product { Id = 1, Name = "Pen", Price = 100, CategoryId = 1, StockQuantity = 50 },
                new Product { Id = 2, Name = "Apple", Price = 200, CategoryId = 2, StockQuantity = 150 },
                new Product { Id = 3, Name = "Apple Pen", Price = 300, CategoryId = 3, StockQuantity = 12 },
            });
        // mockRepo.Object is the fake repo itself, passed in instead of a real one
        var service = new ProductService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync();
        
        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetProductAsync_ReturnsCorrectDto_WhenProductExists()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product
            {
                Id = 1, Name = "Pen", Price = 100, CategoryId = 1, StockQuantity = 50
            });
        var service = new ProductService(mockRepo.Object);

        // Act
        var result = await service.GetProductAsync(1);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Pen", result.Name);
        Assert.Equal(100, result.Price);
        Assert.Equal(1, result.CategoryId);
    }
    
    [Fact]
    public async Task GetProductAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync((Product?)null);
        var service = new ProductService(mockRepo.Object);

        // Act
        var result = await service.GetProductAsync(99);

        // Assert
        Assert.Null(result);
    }
}