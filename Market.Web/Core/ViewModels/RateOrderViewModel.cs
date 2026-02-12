using System.ComponentModel.DataAnnotations;

namespace Market.Web.Core.ViewModels
{
    public class RateOrderViewModel
    {
        public int OrderId { get; set; }

        public string AuctionTitle { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Musisz wybrać ocenę.")]
        [Range(1, 5, ErrorMessage = "Ocena musi być między 1 a 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Napisz krótki komentarz.")]
        [StringLength(1000, ErrorMessage = "Komentarz jest za długi.")]
        public string Comment { get; set; } = string.Empty;
    }
}