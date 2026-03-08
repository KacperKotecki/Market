using Market.Web.Core.Models;

namespace Market.Web.Core.DTOs;

public class AuctionSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public AuctionStatus AuctionStatus { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Quantity { get; set; }               // used in Index.cshtml line 103
    public bool GeneratedByAi { get; set; }         // used in Index.cshtml line 128
    public string? ThumbnailPath { get; set; }      // first AuctionImage.ImagePath or null
    public string SellerUserName { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;  // replaces item.UserId in view
    public bool IsCompanySale { get; set; }
}
