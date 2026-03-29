namespace Market.Web.Core.DTOs;

public class AuctionStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool GeneratedByAi { get; set; }
}