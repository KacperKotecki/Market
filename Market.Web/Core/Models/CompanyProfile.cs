using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Web.Core.Models;

public class CompanyProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Nazwa Firmy")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string NIP { get; set; } = string.Empty;

    [Display(Name = "Firmowy Numer Konta")]
    public string? CompanyIBAN { get; set; }

    public Address InvoiceAddress { get; set; } = new Address();

    public int UserProfileId { get; set; }
    
    [ForeignKey("UserProfileId")]
    public virtual UserProfile? UserProfile { get; set; }
}