using Market.Web.Models;

namespace Market.Web.DTOs;

public class AdminAuctionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public AuctionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BannedNote { get; set; } 
    public List<string> ImagePaths { get; set; } = new();
    
    public string SellerEmail { get; set; } = string.Empty;
    public string SellerDisplayName { get; set; } = string.Empty;

    public DateTime EndDate { get; set; }
    public int Quantity { get; set; }

    public bool IsCompanySale { get; set; } 
    public bool GeneratedByAi { get; set; } 

    public string ShortDescription { get; set; } = string.Empty;

    public bool IsBanned => Status == AuctionStatus.Banned || Status == AuctionStatus.Suspended;
    public bool IsExpired => DateTime.Now > EndDate;
}