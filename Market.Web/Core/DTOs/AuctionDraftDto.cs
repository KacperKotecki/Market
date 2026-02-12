namespace Market.Web.Core.DTOs;

public class AuctionDraftDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public string Category { get; set; } = string.Empty;
}