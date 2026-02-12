using Market.Web.Core.Models;

namespace Market.Web.Core.ViewModels
{
    public class MySaleViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        
        public string AuctionTitle { get; set; } = string.Empty;
        public int AuctionId { get; set; }
        
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        
        public bool IsCompanyPurchase { get; set; }
        public string? ShippingAddressString { get; set; } // Sformatowany adres
        public string? InvoiceDataString { get; set; }     // Sformatowane dane do faktury
    }
}