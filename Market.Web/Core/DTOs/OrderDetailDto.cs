using Market.Web.Core.Models;

namespace Market.Web.Core.DTOs;

public class OrderDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
}
