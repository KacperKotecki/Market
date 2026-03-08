using Market.Web.Core.DTOs;

namespace Market.Web.Core.ViewModels;

public class AuctionListViewModel
{
    public List<AuctionSummaryDto> Auctions { get; set; } = [];

    // Filter state — repopulates the search form after submit
    public string? SearchString { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortOrder { get; set; }

    // Pagination state — UI controls come in plan-feature-auction-pagination-ui.md
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
