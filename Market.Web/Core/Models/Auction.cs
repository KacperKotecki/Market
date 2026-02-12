using System.ComponentModel.DataAnnotations;

namespace Market.Web.Core.Models;

public class Auction
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tytuł jest wymagany.")]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Opis jest wymagany.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cena jest wymagana.")]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Podanie ilości jest wymagane.")]
    [Range(1, 10000, ErrorMessage = "Ilość musi wynosić przynajmniej 1.")]
    [Display(Name = "Ilość sztuk")]
    public int Quantity { get; set; } = 1;
    
    [Required(ErrorMessage = "Kategoria jest wymagana.")]
    public string Category { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; }

    [Display(Name = "Sprzedaż jako firma (Faktura VAT / Paragon)")]
    public bool IsCompanySale { get; set; } = false; 
    public bool GeneratedByAi { get; set; } = false;
    public AuctionStatus AuctionStatus { get; set; }

    public string? BannedNote { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public ICollection<AuctionImage> Images { get; set; } = new List<AuctionImage>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}