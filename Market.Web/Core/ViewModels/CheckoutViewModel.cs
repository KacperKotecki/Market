using Market.Web.Core.Models;

namespace Market.Web.Core.ViewModels
{
    public class CheckoutViewModel
    {
        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string SellerName { get; set; } = string.Empty; 
        public string ImageUrl { get; set; } = string.Empty;   
        
        public bool IsCompanySale { get; set; } 

        // --- DANE KUPUJĄCEGO (Do wysyłki) ---
        public string BuyerName { get; set; } = string.Empty; 
        public Address ShippingAddress { get; set; } = new Address();

        // --- OPCJE ZAKUPU (Decyzje użytkownika) ---
        public bool WantsInvoice { get; set; } // Checkbox: Czy chcę fakturę?
        
        // Czy kupujący w ogóle ma profil firmy? (Służy do blokowania checkboxa)
        public bool BuyerHasCompanyProfile { get; set; } 
        
        // Dane firmy kupującego (tylko do podglądu, pobierane z bazy)
        public string? BuyerCompanyName { get; set; }
        public string? BuyerNIP { get; set; }
        public Address? BuyerInvoiceAddress { get; set; }
    }
}