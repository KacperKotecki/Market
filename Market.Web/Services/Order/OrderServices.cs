using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Repositories;
using Market.Web.Services.Payments;

namespace Market.Web.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProfileService _profileService;
    private readonly IPaymentService _paymentService;

    public OrderService(
        IUnitOfWork unitOfWork, 
        IProfileService profileService,
        IPaymentService paymentService)
    {
        _unitOfWork = unitOfWork;
        _profileService = profileService;
        _paymentService = paymentService;
    }
    public async Task<CheckoutViewModel?> GetCheckoutModelAsync(int auctionId, string userId)
    {
        var auction = await _unitOfWork.Auctions.GetByIdAsync(auctionId);

        if (auction == null || auction.Quantity <= 0 || auction.EndDate <= DateTime.Now) return null;
        if (auction.UserId == userId) return null;

        var userProfile = await _profileService.GetByUserIdAsync(userId);

        return new CheckoutViewModel
        {
            AuctionId = auction.Id,
            AuctionTitle = auction.Title,
            Price = auction.Price,
            IsCompanySale = auction.IsCompanySale,
            SellerName = auction.User?.UserName ?? "Nieznany",
            ImageUrl = auction.Images.FirstOrDefault()?.ImagePath ?? "/img/placeholder.png",
            
            BuyerName = userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : "Kupujący",
            ShippingAddress = userProfile?.ShippingAddress ?? new Address(),
            
            BuyerHasCompanyProfile = userProfile?.CompanyProfile != null,
            BuyerCompanyName = userProfile?.CompanyProfile?.CompanyName,
            BuyerNIP = userProfile?.CompanyProfile?.NIP,
            BuyerInvoiceAddress = userProfile?.CompanyProfile?.InvoiceAddress
        };
    }

    public async Task<List<MyOrderViewModel>> GetBuyerOrdersAsync(string userId)
    {
        var orders = await _unitOfWork.Orders.GetBuyerOrdersAsync(userId);
        return orders.Select(o => new MyOrderViewModel
        {
            OrderId = o.Id,
            OrderDate = o.OrderDate,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            IsCompanyPurchase = o.IsCompanyPurchase,
            AuctionId = o.AuctionId,
            AuctionTitle = o.Auction != null ? o.Auction.Title : "Oferta usunięta",
            SellerName = o.Auction != null && o.Auction.User != null ? o.Auction.User.UserName : "Nieznany",
            HasOpinion = o.Opinion != null,
            MyRating = o.Opinion != null ? o.Opinion.Rating : 0,
            ImageUrl = o.Auction != null && o.Auction.Images.Any() ? o.Auction.Images.First().ImagePath : "https://via.placeholder.com/150"
        }).ToList();
    }

    public async Task<List<MySaleViewModel>> GetSellerSalesAsync(string userId)
    {
        var sales = await _unitOfWork.Orders.GetSellerSalesAsync(userId);
        return sales.Select(o => new MySaleViewModel
        {
            OrderId = o.Id,
            OrderDate = o.OrderDate,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            AuctionId = o.AuctionId,
            AuctionTitle = o.Auction?.Title ?? "Brak tytułu",
            BuyerName = o.Buyer?.UserName ?? "Nieznany",
            BuyerEmail = o.Buyer?.Email ?? "",
            IsCompanyPurchase = o.IsCompanyPurchase,
            ShippingAddressString = o.Buyer?.UserProfile?.ShippingAddress != null
                    ? $"{o.Buyer.UserProfile.ShippingAddress.Street}, {o.Buyer.UserProfile.ShippingAddress.PostalCode} {o.Buyer.UserProfile.ShippingAddress.City}"
                    : "Brak danych adresowych",
            InvoiceDataString = o.IsCompanyPurchase 
                    ? $"{o.BuyerCompanyName} (NIP: {o.BuyerNIP})\n{o.BuyerInvoiceAddress}" 
                    : null
        }).ToList();
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _unitOfWork.Orders.GetByIdAsync(orderId);
    }
    
    public async Task<RateOrderViewModel?> GetRateOrderModelAsync(int orderId, string userId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.BuyerId != userId) return null;
        
        return new RateOrderViewModel
        {
            OrderId = order.Id,
            AuctionTitle = order.Auction?.Title ?? "Usunięta",
            SellerName = order.Auction?.User?.UserName ?? "Nieznany",
            ImageUrl = order.Auction?.Images?.FirstOrDefault()?.ImagePath ?? "https://via.placeholder.com/150"
        };
    }
    public async Task<string> PlaceOrderAsync(CheckoutViewModel model, string userId, string domain)
    {
        var auction = await _unitOfWork.Auctions.GetByIdAsync(model.AuctionId);
        if (auction == null || auction.Quantity <= 0) throw new InvalidOperationException("Aukcja niedostępna.");

        var userProfile = await _profileService.GetByUserIdAsync(userId);

        if (model.WantsInvoice && userProfile?.CompanyProfile == null)
            throw new ArgumentException("Brak danych firmy do faktury.");

        var order = new Order
        {
            AuctionId = auction.Id,
            BuyerId = userId,
            TotalPrice = auction.Price,
            OrderDate = DateTime.Now,
            Status = OrderStatus.Pending,
            IsCompanyPurchase = model.WantsInvoice
        };

        if (model.WantsInvoice && userProfile?.CompanyProfile != null)
        {
            var cp = userProfile.CompanyProfile;
            order.BuyerCompanyName = cp.CompanyName;
            order.BuyerNIP = cp.NIP;
            order.BuyerInvoiceAddress = $"{cp.InvoiceAddress.Street}, {cp.InvoiceAddress.PostalCode} {cp.InvoiceAddress.City}";
        }

        auction.Quantity -= 1;
        if (auction.Quantity == 0) auction.AuctionStatus = AuctionStatus.Sold;

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.CompleteAsync();

        try
        {
            return await _paymentService.CreateCheckoutSession(order, domain);
        }
        catch (Exception)
        {
            auction.Quantity += 1;
            if (auction.AuctionStatus == AuctionStatus.Sold) auction.AuctionStatus = AuctionStatus.Active;
            
            _unitOfWork.Orders.Remove(order); 

            await _unitOfWork.CompleteAsync();
            throw;
        }
    }

    public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string sellerId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.Auction == null || order.Auction.UserId != sellerId) 
            throw new UnauthorizedAccessException("Brak uprawnień do zamówienia.");

        order.Status = newStatus;
        await _unitOfWork.CompleteAsync();
    }

    public async Task ConfirmDeliveryAsync(int orderId, string buyerId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.BuyerId != buyerId)
            throw new UnauthorizedAccessException("Brak uprawnień do zamówienia.");

        if (order.Status != OrderStatus.Shipped)
             throw new InvalidOperationException("Można potwierdzić tylko wysłane zamówienia.");

        order.Status = OrderStatus.Completed;

        if(order.Auction?.User?.UserProfile != null)
        {
            order.Auction.User.UserProfile.WalletBalance += order.TotalPrice;
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task AddOpinionAsync(RateOrderViewModel model, string buyerId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(model.OrderId);
        if (order == null || order.BuyerId != buyerId) throw new UnauthorizedAccessException();
        if (order.Opinion != null) throw new InvalidOperationException("Już oceniono.");

        var opinion = new Opinion
        {
            OrderId = order.Id,
            BuyerId = buyerId,
            SellerId = order.Auction!.UserId,
            Comment = model.Comment,
            Rating = model.Rating,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Orders.AddOpinionAsync(opinion);
        await _unitOfWork.CompleteAsync();
    }
}