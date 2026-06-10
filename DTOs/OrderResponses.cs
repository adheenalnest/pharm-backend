namespace PharmeasyAPI.DTOs;

public class OrderProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountedPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
}

public class OrderCustomerDetailsDto
{
    public int CustomerProfileId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class OrderResponseDto
{
    public int Id { get; set; }
    public OrderProductDetailsDto Product { get; set; } = new OrderProductDetailsDto();
    public OrderCustomerDetailsDto Customer { get; set; } = new OrderCustomerDetailsDto();
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
