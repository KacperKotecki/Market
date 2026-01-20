using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Web.Models;

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Prywatny Numer Konta")]
    public string? PrivateIBAN { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WalletBalance { get; set; } = 0;
    
    public Address ShippingAddress { get; set; } = new Address();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    public virtual CompanyProfile? CompanyProfile { get; set; }
}