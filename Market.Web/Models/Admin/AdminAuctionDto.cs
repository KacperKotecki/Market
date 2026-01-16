using Market.Web.Models; // Dla AuctionStatus

namespace Market.Web.Models.Admin;

public class AdminAuctionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SellerEmail { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> ImagePaths { get; set; } = new();
    public AuctionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BannedNote { get; set; } 
    
    public bool IsBanned => Status == AuctionStatus.Banned || Status == AuctionStatus.Suspended;
}