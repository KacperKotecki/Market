using System;

namespace Market.Web.Core.DTOs;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    
    public int AuctionCount { get; set; }
    
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }

    public string? PrivateIBAN { get; set; }
    public string AddressStreet { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressPostalCode { get; set; } = string.Empty;

    public bool HasCompany { get; set; }
    public string? CompanyName { get; set; }
    public string? NIP { get; set; }
    public string? CompanyIBAN { get; set; }
    public string? CompanyAddress { get; set; } // Sformatowany adres
}