using Moq;
using NUnit.Framework;
using FluentAssertions; 
using Market.Web.Services;
using Market.Web.Core.Models;
using Market.Web.Repositories;
using Market.Web.Services.Payments;
using Market.Web.Core.ViewModels;

namespace Market.Tests;

[TestFixture]
public class OrderServiceTests
{
    private Mock<IUnitOfWork> _unitofWorkMock;
    private Mock<IProfileService> _profileServiceMock;
    private Mock<IPaymentService> _paymentServuiceMock; 
    
    private OrderService _orderService;
    [SetUp]
    public void Setup()
    {
    _unitofWorkMock = new Mock<IUnitOfWork>();
    _profileServiceMock = new Mock<IProfileService>();
    _paymentServuiceMock = new Mock<IPaymentService>();

    _orderService = new OrderService(_unitofWorkMock.Object, _profileServiceMock.Object, _paymentServuiceMock.Object);
    }
    // public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string sellerId)
    // {
    //     var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
    //     if (order == null || order.Auction == null || order.Auction.UserId != sellerId) 
    //         throw new UnauthorizedAccessException("Brak uprawnień do zamówienia.");

    //     order.Status = newStatus;
    //     await _unitOfWork.CompleteAsync();
    // }
    [Test]
    public async Task UpdateOrderStatusAsync_ShouldChangeStatusToInput_WhenUserIsSeller()
    {
        var inputStatus = OrderStatus.Pending;
        var expectedStatus = OrderStatus.Paid;
        var sellerId = "sellerid1";
        var order = new Order
        {
            Id = 1,
            Status = inputStatus,
            Auction = new Auction
            {
                UserId = sellerId,
            },
        };
       
        _unitofWorkMock.Setup(u => u.Orders.GetByIdAsync(order.Id)).ReturnsAsync(order);

        await _orderService.UpdateOrderStatusAsync(1,expectedStatus,sellerId);

        order.Status.Should().Be(expectedStatus); 

        _unitofWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotSeller()
    {
        var inputStatus = OrderStatus.Pending;
        var expectedStatus = OrderStatus.Paid;
        var sellerId = "sellerid1";
        var notsellerid = "inncorectId";
        var order = new Order
        {
            Id = 1,
            Status = inputStatus,
            Auction = new Auction
            {
                UserId = sellerId,
            },
        };
       
        _unitofWorkMock.Setup(u => u.Orders.GetByIdAsync(order.Id)).ReturnsAsync(order);

        Func<Task> action = async() => await _orderService.UpdateOrderStatusAsync(1, expectedStatus, notsellerid);

        action.Should().ThrowAsync<UnauthorizedAccessException>(); 

        _unitofWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task MarkOrderAsPaidAsync_ShouldChangeStatusToPaid_WhenOrderIsPending()
    {
        var statusPending = OrderStatus.Pending;
        var orderId = 1;

        var order = new Order
        {
            Id = orderId,
            Status = statusPending
        };

        _unitofWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _orderService.MarkOrderAsPaidAsync(orderId);

        order.Status.Should().Be(OrderStatus.Paid);

        _unitofWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task MarkOrderAsPaidAsync_ShouldDontChangeStatusToPaid_WhenOrderIsNotPending()
    {
        var orderId = 1;
        var statusNotPending = OrderStatus.Cancelled;

        var order = new Order
        {
          Id = orderId,
          Status = statusNotPending  
        };

        _unitofWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _orderService.MarkOrderAsPaidAsync(orderId);

        order.Status.Should().Be(statusNotPending);

        _unitofWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task MarkOrderAsPaidAsync_ShouldDoNothing_WhenOrderIsNull()
    {
        var orderId = 999;
        var statusInput = OrderStatus.Pending;

        Order nullOrder = null;

        _unitofWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(nullOrder);

        await _orderService.MarkOrderAsPaidAsync(orderId);

        _unitofWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Test]
    public async Task AddOpinionAsync_ShouldAddOpinion_WhenItIsYourOrder()
    {
        var orderId = 1;
        var buyerId = "buyerid";
        var sellerId = "sellerid";

        var model = new RateOrderViewModel
        {
            OrderId = orderId,
            AuctionTitle = "Title",
            SellerName = "Name",
            ImageUrl = "URL",
            Rating = 1,
            Comment = "Good seller"
        };

        var order = new Order
        {
            Id = orderId,
            BuyerId = buyerId,
            Auction = new Auction
            {
                UserId = sellerId
            },
            Opinion = null

        };


        _unitofWorkMock.Setup(u => u.Orders.GetByIdAsync(orderId)).ReturnsAsync(order);

        await _orderService.AddOpinionAsync(model, buyerId);

        _unitofWorkMock.Verify(u => u.CompleteAsync(), Times.Once);

                _unitofWorkMock.Verify(u => u.Orders.AddOpinionAsync(It.Is<Opinion>(op => 
            op.OrderId == orderId &&
            op.BuyerId == buyerId &&
            op.SellerId == sellerId && // Sprawdzamy czy pobrał ID z aukcji
            op.Rating == model.Rating &&
            op.Comment == model.Comment
        )), Times.Once);
    }
}