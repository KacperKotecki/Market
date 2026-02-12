using Market.Web.Core.Models;

namespace Market.Web.Core.ViewModels
{
    public class MyOrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }

        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        
        public bool IsCompanyPurchase { get; set; }

        public string SellerName { get; set; } = string.Empty;

        public bool HasOpinion { get; set; }
        public int MyRating { get; set; }
    }
}