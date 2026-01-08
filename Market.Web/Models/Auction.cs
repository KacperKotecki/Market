using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Market.Web.Models;

public class Auction
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tytuł jest wymagany.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tytuł musie mieć od 3 do 100 znaków")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategoria jest wymagana.")]
    public string Category { get; set; } = string.Empty;
    [Required(ErrorMessage = "Opis jest wymagany.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000.00, ErrorMessage = "Podaj prawidłową cenę.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    [Display(Name = "Zdjęcie URL")]
    public string? ImageUrl { get; set; }

    public AuctionStatus AuctionStatus { get; set; } = AuctionStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; } 

    public string? BuyerId { get; set; }
}