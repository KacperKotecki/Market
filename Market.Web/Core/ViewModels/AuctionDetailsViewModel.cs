using System;
using Market.Web.Core.Models;

namespace Market.Web.Core.ViewModels;

public class AuctionDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCompanySale { get; set; }
    public bool GeneratedByAi { get; set; }
    public AuctionStatus AuctionStatus { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public List<string> ImagePaths { get; set; } = [];
}