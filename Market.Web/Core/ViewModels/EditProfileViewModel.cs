using System.ComponentModel.DataAnnotations;
using Market.Web.Core.Models;

namespace Market.Web.Core.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Imię jest wymagane")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        public string LastName { get; set; } = string.Empty;

        public Address ShippingAddress { get; set; } = new Address();

        [Display(Name = "Prywatny numer konta (IBAN)")]
        public string? PrivateIBAN { get; set; }

        [Display(Name = "Chcę podać dane firmowe / fakturę")]
        public bool HasCompanyProfile { get; set; }

        public string? CompanyName { get; set; }
        public string? NIP { get; set; }
        public string? CompanyIBAN { get; set; }
        public Address InvoiceAddress { get; set; } = new Address();
    }
}