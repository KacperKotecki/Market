using Market.Web.Models.Admin; 

namespace Market.Web.ViewModels;

public class AdminAuctionsListViewModel
{
    public List<AdminAuctionDto> Auctions { get; set; } = new();
    public string? SearchString { get; set; }
    public string? SortOrder { get; set; }
    
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}