namespace Market.Web.Core.Models;

public class AuctionImage
{
    public int Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;

    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
}