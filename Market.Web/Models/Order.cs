using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Web.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public int AuctionId { get; set; }
        
        [ForeignKey("AuctionId")]
        public virtual Auction? Auction { get; set; }

        [Required]
        public string BuyerId { get; set; } = string.Empty;

        [ForeignKey("BuyerId")]
        public virtual ApplicationUser? Buyer { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public bool IsCompanyPurchase { get; set; } = false;

        public string? BuyerCompanyName { get; set; }
        public string? BuyerNIP { get; set; }
        public string? BuyerInvoiceAddress { get; set; } // Zapiszemy jako jeden ciąg lub JSON dla uproszczenia
    }

    public enum OrderStatus
    {
        Pending = 0,   // Oczekujace
        Paid = 1,      // Opłacone
        Shipped = 2,   // Wysłane
        Completed = 3, // Zakończone
        Cancelled = 4  // Anulowane
    }
}