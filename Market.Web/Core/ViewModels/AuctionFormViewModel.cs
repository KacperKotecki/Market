using System.ComponentModel.DataAnnotations;

namespace Market.Web.Core.ViewModels;

public class AuctionFormViewModel
{
    public int Id { get; set; }  // 0 for Create, actual Id for Edit

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

    public DateTime EndDate { get; set; }

    [Display(Name = "Sprzedaż jako firma (Faktura VAT / Paragon)")]
    public bool IsCompanySale { get; set; }

    public bool GeneratedByAi { get; set; }

    // Populated for Edit only — empty list for Create
    public List<string> ImagePaths { get; set; } = [];

    // Populated for Edit only, used for ownership check — no asp-for binding so never form-posted
    public string SellerId { get; set; } = string.Empty;
}
