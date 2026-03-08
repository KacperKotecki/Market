using Market.Web.Core.Models;

namespace Market.Web.Core.DTOs;

public class AuctionFilter
{
    public string? SearchString { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortOrder { get; set; }     // "price_asc" | "price_desc" | null = newest first
    public AuctionStatus? Status { get; set; }  // null = Active only (public listing default)
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
