namespace Market.Web.Models
{
    public enum AuctionStatus
    {
    Draft = 0,  // szkic      
    Active = 1,   // aktywnie wyświetlana na liście   
    Sold = 2,        // sprzedana
    Expired = 3,     // po terminie
    Cancelled = 4,   // anulowana przez sprzedawcę
    Suspended = 5,    // zawieszona przez administratora
    Banned = 6 // zablokowana
    }
}