using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Web.Core.Models
{
    public class Opinion
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } 

        [StringLength(1000, ErrorMessage = "Opinia nie może być dłuższa niż 1000 znaków.")]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int OrderId { get; set; }
        
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        public string BuyerId { get; set; } = string.Empty;
        
        [ForeignKey("BuyerId")]
        public virtual ApplicationUser? Buyer { get; set; }

        [Required]
        public string SellerId { get; set; } = string.Empty;
        
        [ForeignKey("SellerId")]
        public virtual ApplicationUser? Seller { get; set; }
    }
}