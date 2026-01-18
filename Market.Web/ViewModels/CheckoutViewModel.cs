using Market.Web.Models;

namespace Market.Web.ViewModels
{
    public class CheckoutViewModel
    {
        // --- DANE AUKCJI ---
        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string SellerName { get; set; } = string.Empty; // Dodajmy to, przyda się w widoku
        public string ImageUrl { get; set; } = string.Empty;   // Dodajmy to dla lepszego UX
        
        // Czy sprzedający wystawia fakturę? (Info dla kupującego)
        public bool IsCompanySale { get; set; } 

        // --- DANE KUPUJĄCEGO (Do wysyłki) ---
        public string BuyerName { get; set; } = string.Empty; // Do wyświetlenia: Jan Kowalski
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