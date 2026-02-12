using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Market.Web.Core.Models;

[Owned] 
public class Address
{
    [Display(Name = "Ulica i numer")]
    public string Street { get; set; } = string.Empty;

    [Display(Name = "Miasto")]
    public string City { get; set; } = string.Empty;

    [Display(Name = "Kod pocztowy")]
    [DataType(DataType.PostalCode)]
    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = "Polska";
}