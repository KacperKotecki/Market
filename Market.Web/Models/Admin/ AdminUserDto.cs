namespace Market.Web.Models.Admin;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // Imię + Nazwisko
    public string? CompanyName { get; set; } // Opcjonalnie nazwa firmy
    public string? NIP { get; set; }
    public bool IsBlocked { get; set; }
    public int AuctionCount { get; set; } // Ile ma aukcji
}