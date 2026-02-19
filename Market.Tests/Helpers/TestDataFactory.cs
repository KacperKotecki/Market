using Market.Web.Core.Models;

namespace Market.Tests;

public static class TestDataFactory
{

    public static Address CreateAddress(
        string street = "Default St 1", 
        string city = "Warszawa", 
        string postalCode = "00-001", 
        string country = "Polska")
    {
        return new Address
        {
            Street = street,
            City = city,
            PostalCode = postalCode,
            Country = country
        };
    }
    
    public static UserProfile CreateUserProfile(
        string userId = "userId", 
        int id = 1,
        Address shippingAddress = null
    )
    {
        return new UserProfile
        {
            Id = id,
            UserId = userId,
            FirstName = "Jan",
            LastName = "Kowalski",
            PrivateIBAN = "PL00000000000000000000000000",
            WalletBalance = 0, 
            ShippingAddress = shippingAddress ?? CreateAddress() 
        };
    }

    public static Auction CreateAuction(
        int id = 1, 
        string userId = "sellerId", 
        ApplicationUser user = null)
    {
        var actualUser = user ?? new ApplicationUser 
        { 
            Id = userId,
            UserName = "SellerName",
            UserProfile = CreateUserProfile(userId: userId) 
        };

        return new Auction
        {
            Id = id,
            Title = "Default Auction",
            Description = "Default Description",
            Price = 100, 
            Quantity = 1,
            AuctionStatus = AuctionStatus.Active, 

            UserId = userId,
            User = actualUser, 

            Category = "General",
            CreatedAt = DateTime.Now,
            EndDate = DateTime.Now.AddDays(7),
            Images = new List<AuctionImage>()
        };
    }

    public static Order CreateOrder(
        int id = 1, 
        Auction auction = null, 
        ApplicationUser buyer = null 
    )
    {
        var actualAuction = auction ?? CreateAuction();

        var buyerId = buyer?.Id ?? "buyerId";
        var actualBuyer = buyer ?? new ApplicationUser 
        { 
            Id = buyerId,
            UserName = "BuyerName",
            UserProfile = CreateUserProfile(userId: buyerId) 
        };

        return new Order
        {
            Id = id,

            TotalPrice = actualAuction.Price, 
            Status = OrderStatus.Pending,
            OrderDate = DateTime.Now,
            
            AuctionId = actualAuction.Id,
            Auction = actualAuction, 
            
            BuyerId = buyerId,
            Buyer = actualBuyer
        };
    }
}