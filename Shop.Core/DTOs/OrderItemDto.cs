namespace Shop.Core.DTOs;

public record OrderItemDto(int Id, int ProductId, int Quantity, decimal UnitPrice);