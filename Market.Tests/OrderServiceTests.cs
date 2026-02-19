using Moq;
using NUnit.Framework;
using FluentAssertions; 
using Market.Web.Services;
using Market.Web.Core.Models;
using Market.Web.Repositories;
using Market.Web.Core.ViewModels;

namespace Market.Tests;

[TestFixture]
public class OrderServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IProfileService> _profileServiceMock;
    private Mock<IPaymentService> _paymentServiceMock; 
    
    private OrderService _orderService;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _profileServiceMock = new Mock<IProfileService>();
        _paymentServiceMock = new Mock<IPaymentService>();

        var auctionRepoMock = new Mock<IAuctionRepository>(); 
        _unitOfWorkMock.Setup(u => u.Auctions).Returns(auctionRepoMock.Object);

        var orderRepoMock = new Mock<IOrderRepository>(); 
        
        orderRepoMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
                     .Returns(Task.CompletedTask);
        
        orderRepoMock.Setup(x => x.AddOpinionAsync(It.IsAny<Opinion>()))
                     .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Orders).Returns(orderRepoMock.Object);

        _orderService = new OrderService(_unitOfWorkMock.Object, _profileServiceMock.Object, _paymentServiceMock.Object);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ShouldChangeStatusToInput_WhenUserIsSeller()
    {
        // Arrange
        var expectedStatus = OrderStatus.Paid;
        var sellerId = "sellerId";
        
        var auction = TestDataFactory.CreateAuction(userId: sellerId);
        var order = TestDataFactory.CreateOrder(id: 1, auction: auction);
        
        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(order.Id)).ReturnsAsync(order);

        // Act
        await _orderService.UpdateOrderStatusAsync(order.Id, expectedStatus, sellerId);

        // Assert
        order.Status.Should().Be(expectedStatus); 
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotSeller()
    {
        // Arrange
        var sellerId = "sellerId";
        var otherUserId = "otherUserId"; // Ktoś kto próbuje zmienić status, ale nie jest sprzedawcą

        var auction = TestDataFactory.CreateAuction(userId: sellerId);
        var order = TestDataFactory.CreateOrder(id: 1, auction: auction);
       
        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(order.Id)).ReturnsAsync(order);

        // Act
        Func<Task> action = async() => await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Paid, otherUserId);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedAccessException>(); 
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task MarkOrderAsPaidAsync_ShouldChangeStatusToPaid_WhenOrderIsPending()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(id: 1);
        order.Status = OrderStatus.Pending; // Upewniamy się, że status jest Pending

        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(order.Id)).ReturnsAsync(order);

        // Act
        await _orderService.MarkOrderAsPaidAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.Paid);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task MarkOrderAsPaidAsync_ShouldNotChangeStatus_WhenOrderIsNotPending()
    {
        // Arrange
        var initialStatus = OrderStatus.Cancelled;
        var order = TestDataFactory.CreateOrder(id: 1);
        order.Status = initialStatus;

        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(order.Id)).ReturnsAsync(order);

        // Act
        await _orderService.MarkOrderAsPaidAsync(order.Id);

        // Assert
        order.Status.Should().Be(initialStatus);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task MarkOrderAsPaidAsync_ShouldDoNothing_WhenOrderDefaultsToNull()
    {
        // Arrange
        var orderId = 999;
        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync((Order)null);

        // Act
        await _orderService.MarkOrderAsPaidAsync(orderId);

        // Assert
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task AddOpinionAsync_ShouldAddOpinion_WhenItIsYourOrder()
    {
        // Arrange
        var orderId = 1;
        var buyerId = "buyer1";
        var sellerId = "seller1";

        var auction = TestDataFactory.CreateAuction(userId: sellerId);
        var order = TestDataFactory.CreateOrder(id: orderId, auction: auction);
        order.BuyerId = buyerId;
        order.Opinion = null; 

        var model = new RateOrderViewModel
        {
            OrderId = orderId,
            Rating = 5,
            Comment = "Great service!"
        };

        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        await _orderService.AddOpinionAsync(model, buyerId);

        // Assert
        _unitOfWorkMock.Verify(u => u.Orders.AddOpinionAsync(It.Is<Opinion>(op => 
            op.OrderId == orderId &&
            op.BuyerId == buyerId &&
            op.SellerId == sellerId &&
            op.Rating == model.Rating &&
            op.Comment == model.Comment
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetCheckoutModelAsync_ShouldCreateViewModel_WhenAuctionAndUserExists()
    {
        // Arrange
        var auctionId = 1;
        var userId = "user1";
        
        var auction = TestDataFactory.CreateAuction(id: auctionId, userId: "seller1");
        auction.EndDate = DateTime.Now.AddDays(10);
        auction.Quantity = 1;

        var userProfile = TestDataFactory.CreateUserProfile(userId: userId);

        _unitOfWorkMock.Setup(u => u.Auctions.GetByIdAsync(auctionId)).ReturnsAsync(auction);
        _profileServiceMock.Setup(p => p.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        // Act
        var viewModel = await _orderService.GetCheckoutModelAsync(auctionId, userId);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel!.AuctionId.Should().Be(auctionId);
        viewModel.AuctionTitle.Should().Be(auction.Title);
        viewModel.BuyerName.Should().Contain("Kowalski");
        
        viewModel.ShippingAddress.Should().BeEquivalentTo(userProfile.ShippingAddress);
    }

    [Test]
    public async Task GetCheckoutModelAsync_ShouldReturnNull_WhenAuctionIsSoldOut()
    {
        // Arrange
        var auctionId = 1;
        var userId = "user1";

        var auction = TestDataFactory.CreateAuction(id: auctionId);
        auction.Quantity = 0; 

        _unitOfWorkMock.Setup(u => u.Auctions.GetByIdAsync(auctionId)).ReturnsAsync(auction);

        // Act
        var viewModel = await _orderService.GetCheckoutModelAsync(auctionId, userId);

        // Assert
        viewModel.Should().BeNull();
    }

    [Test]
    public async Task PlaceOrderAsync_ShouldCreateOrderAndDecrementsQuantity_WhenDataIsCorrect()
    {
        // Arrange
        var auctionId = 10; 
        var buyerId = "buyerId";
        var expectedPaymentUrl = "http://payment.url";
        
        var auction = TestDataFactory.CreateAuction(id: auctionId);
        auction.Quantity = 5; 
        auction.Price = 200;

        var buyerProfile = TestDataFactory.CreateUserProfile(userId: buyerId);
        
        _unitOfWorkMock.Setup(u => u.Auctions.GetByIdAsync(auctionId)).ReturnsAsync(auction);
        _profileServiceMock.Setup(p => p.GetByUserIdAsync(buyerId)).ReturnsAsync(buyerProfile);

        _paymentServiceMock
            .Setup(p => p.CreateCheckoutSession(It.IsAny<Order>(), It.IsAny<string>()))
            .ReturnsAsync(expectedPaymentUrl);

        var model = new CheckoutViewModel
        {
            AuctionId = auctionId,
            WantsInvoice = false
        };

        // Act
        var resultUrl = await _orderService.PlaceOrderAsync(model, buyerId, "http://localhost");

        // Assert
        resultUrl.Should().Be(expectedPaymentUrl);

        auction.Quantity.Should().Be(4); 

        auction.AuctionStatus.Should().Be(AuctionStatus.Active);

        _unitOfWorkMock.Verify(u => u.Orders.AddAsync(It.Is<Order>(o => 
            o.AuctionId == auctionId && 
            o.BuyerId == buyerId &&
            o.TotalPrice == 200
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once); // SaveChanges
    }

    [Test]
    public async Task PlaceOrderAsync_ShouldSetStatusToSold_WhenLastItemIsPurchased()
    {
        // Arrange
        var auctionId = 10; 
        var buyerId = "buyerId";
        
        var auction = TestDataFactory.CreateAuction(id: auctionId);
        auction.Quantity = 1; 

        _unitOfWorkMock.Setup(u => u.Auctions.GetByIdAsync(auctionId)).ReturnsAsync(auction);
        _profileServiceMock.Setup(p => p.GetByUserIdAsync(buyerId)).ReturnsAsync(TestDataFactory.CreateUserProfile());
        _paymentServiceMock.Setup(p => p.CreateCheckoutSession(It.IsAny<Order>(), It.IsAny<string>())).ReturnsAsync("url");

        var model = new CheckoutViewModel { AuctionId = auctionId };

        // Act
        await _orderService.PlaceOrderAsync(model, buyerId, "domain");

        // Assert
        auction.Quantity.Should().Be(0);
        auction.AuctionStatus.Should().Be(AuctionStatus.Sold);
    }

    [Test]
    public async Task ConfirmDeliveryAsync_ShouldAddFundsToSellerWallet_WhenOrderIsShipped()
    {
        // Arrange
        var orderId = 1;
        var buyerId = "buyer1";
        var sellerId = "seller1";

        var auction = TestDataFactory.CreateAuction(id: 1, userId: sellerId);
        
        auction.User.UserProfile.WalletBalance = 0;

        var order = TestDataFactory.CreateOrder(id: orderId, auction: auction);
        order.BuyerId = buyerId;
        order.Status = OrderStatus.Shipped; 
        order.TotalPrice = 150m;           

        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        await _orderService.ConfirmDeliveryAsync(orderId, buyerId);

        // Assert
        order.Status.Should().Be(OrderStatus.Completed);
        
        order.Auction.User.UserProfile.WalletBalance.Should().Be(150m);
        
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task ConfirmDeliveryAsync_ShouldThrowException_WhenOrderIsNotShipped()
    {
        // Arrange
        var orderId = 1;
        var buyerId = "buyer1";
        
        var order = TestDataFactory.CreateOrder(id: orderId);
        order.BuyerId = buyerId;
        order.Status = OrderStatus.Pending; 

        _unitOfWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act & Assert
        Func<Task> action = async () => await _orderService.ConfirmDeliveryAsync(orderId, buyerId);
        
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tylko wysłane*"); 
    }
}