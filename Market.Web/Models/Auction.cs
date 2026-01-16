using System.ComponentModel.DataAnnotations;

namespace Market.Web.Models;

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
    
    [Required(ErrorMessage = "Kategoria jest wymagana.")]
    public string Category { get; set; } = string.Empty;
    
    public ICollection<AuctionImage> Images { get; set; } = new List<AuctionImage>();

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; }

    public AuctionStatus AuctionStatus { get; set; }

    public string? BannedNote { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}