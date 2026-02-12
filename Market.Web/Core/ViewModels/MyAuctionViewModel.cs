using Market.Web.Core.Models;

namespace Market.Web.Core.ViewModels;

public class OpinionDetailViewModel
{
    public string BuyerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string DateFormatted { get; set; } = string.Empty;
}

public class MyAuctionViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    
    public string ImageUrl { get; set; } = string.Empty;
    public string CreatedAtFormatted { get; set; } = string.Empty;
    public string EndDateFormatted { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    public bool IsSoldOut => Quantity <= 0;
    public AuctionStatus AuctionStatus { get; set; }
    public bool IsCompanySale { get; set; }
    public bool GeneratedByAi { get; set; }

    public string StatusBadgeClass { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    
    public bool IsBannedOrSuspended { get; set; }
    public string? BannedNote { get; set; }

    public List<OpinionDetailViewModel> Opinions { get; set; } = new List<OpinionDetailViewModel>();
    
    public int OpinionsCount => Opinions.Count;
    public double AverageRating => Opinions.Any() ? Opinions.Average(o => o.Rating) : 0;
}