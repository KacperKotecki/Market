using FluentAssertions;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Repositories;
using Market.Web.Services;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Moq;
using NUnit.Framework.Internal;

namespace Market.Tests;

[TestFixture]
public class ProfilServiceTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private ProfileService _profileService;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _profileService = new ProfileService(_unitOfWorkMock.Object);
    }

    #region GetByUserIdAsync
    // [Test]
    // public async Task GetByUserIdAsync_ShouldReturnUserProfile_WhenUserExists()
    // {
    // }

    // [Test]
    // public async Task GetByUserIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    // {
    // }
    #endregion

    #region GetEditProfileViewModelAsync
    // [Test]
    // public async Task GetEditProfileViewModelAsync_ShouldReturnViewModelWithData_WhenUserExists()
    

    // [Test]
    // public async Task GetEditProfileViewModelAsync_ShouldReturnEmptyViewModel_WhenUserDoesNotExist()
    
    #endregion

    #region UpdateProfileAsync
    // [Test]
    // public async Task UpdateProfileAsync_ShouldCreateNewProfile_WhenUserDoesNotExist()
    // Sprawdza, czy wywoływane jest _unitOfWork.Profiles.AddAsync, gdy użytkownik nie miał wcześniej profilu.

    // [Test]
    // public async Task UpdateProfileAsync_ShouldUpdateExistingProfile_WhenUserExists()
    // Sprawdza, czy właściwości (FirstName, LastName, IBAN) są aktualizowane i czy wywoływane jest CompleteAsync.

    [Test]
    public async Task UpdateProfileAsync_ShouldAddCompanyProfile_WhenHasCompanyProfileIsTrue()
    {
        var userId = "userID";
        var userProfile = TestDataFactory.CreateUserProfile(userId);
        userProfile.CompanyProfile = null;

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        var model = new EditProfileViewModel
        {
            FirstName = "Anna",
            LastName = "Nowak",
            PrivateIBAN = "PL99999999999999999999999999",
            ShippingAddress = TestDataFactory.CreateAddress("Wall Streat 333a", "New York", "99-999","France"),
            HasCompanyProfile = true,
            CompanyName = "Firma Anna Nowak sp.zop",
            NIP = "1234567890",
            CompanyIBAN = "PL11111111111111111111111111",
            InvoiceAddress = TestDataFactory.CreateAddress("Konstytucyjna 17b", "Wrocław", "55-555","Spain"),
        };

        await _profileService.UpdateProfileAsync(userId, model);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);

        userProfile.CompanyProfile.Should().NotBeNull();
        userProfile.CompanyProfile.CompanyName.Should().Be("Firma Anna Nowak sp.zop");
        userProfile.CompanyProfile.NIP.Should().Be("1234567890");
        userProfile.CompanyProfile.CompanyIBAN.Should().Be("PL11111111111111111111111111");
        userProfile.CompanyProfile.InvoiceAddress.Street.Should().Be("Konstytucyjna 17b");
        userProfile.CompanyProfile.InvoiceAddress.City.Should().Be("Wrocław");
        userProfile.CompanyProfile.InvoiceAddress.PostalCode.Should().Be("55-555");
        userProfile.CompanyProfile.InvoiceAddress.Country.Should().Be("Spain");

        userProfile.FirstName.Should().Be("Anna");
        userProfile.LastName.Should().Be("Nowak");
        userProfile.PrivateIBAN.Should().Be("PL99999999999999999999999999");
        userProfile.ShippingAddress.Street.Should().Be("Wall Streat 333a");
        userProfile.ShippingAddress.City.Should().Be("New York");
        userProfile.ShippingAddress.PostalCode.Should().Be("99-999");
        userProfile.ShippingAddress.Country.Should().Be("France");
    }


    [Test]
    public async Task UpdateProfileAsync_ShouldRemoveCompanyProfile_WhenHasCompanyProfileIsFalse()
    {
        var userId = "userID";
        var userProfile = TestDataFactory.CreateUserProfile(userId);

        userProfile.CompanyProfile = new CompanyProfile(); 
        userProfile.CompanyProfile.CompanyName = "Firma Anna Nowak sp.zop";
        userProfile.CompanyProfile.NIP = "1234567890"!;
        userProfile.CompanyProfile.CompanyIBAN = "PL11111111111111111111111111";
        userProfile.CompanyProfile.InvoiceAddress = TestDataFactory.CreateAddress("Wall Streat 333a");

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        var model = new EditProfileViewModel
        {
            FirstName = "Anna",
            LastName = "Nowak",
            PrivateIBAN = "PL99999999999999999999999999",
            ShippingAddress = TestDataFactory.CreateAddress("Wall Streat 333a", "New York", "99-999","France"),
            HasCompanyProfile = false,
        };

        await _profileService.UpdateProfileAsync(userId, model);

        _unitOfWorkMock.Verify(u => u.Profiles.RemoveCompanyProfile(It.IsAny<CompanyProfile>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);

        userProfile.CompanyProfile.Should().BeNull();

        userProfile.FirstName.Should().Be("Anna");
        userProfile.LastName.Should().Be("Nowak");
        userProfile.PrivateIBAN.Should().Be("PL99999999999999999999999999");
        userProfile.ShippingAddress.Street.Should().Be("Wall Streat 333a");
        userProfile.ShippingAddress.City.Should().Be("New York");
        userProfile.ShippingAddress.PostalCode.Should().Be("99-999");
        userProfile.ShippingAddress.Country.Should().Be("France");
    }
    #endregion

    #region GetFinancesViewModelAsync

    [Test]
    public async Task GetFinancesViewModelAsync_ShouldCalculatePendingFundsCorrectly_WhenOrdersArePaidOrShipped()
    {
        var userId = "userId";
        var userProfile = TestDataFactory.CreateUserProfile(userId);
        
        var orderPending = TestDataFactory.CreateOrder(1);
        orderPending.Status = OrderStatus.Pending;
        orderPending.TotalPrice = 100;

        var orderCancelled = TestDataFactory.CreateOrder(2);
        orderCancelled.Status = OrderStatus.Cancelled;
        orderCancelled.TotalPrice = 100;

        var orderCompleted = TestDataFactory.CreateOrder(3);
        orderCompleted.Status = OrderStatus.Completed;
        orderCompleted.TotalPrice = 100;

        var orderPaid = TestDataFactory.CreateOrder(4);
        orderPaid.Status = OrderStatus.Paid;
        orderPaid.TotalPrice = 7;

        var orderShipped = TestDataFactory.CreateOrder(5);
        orderShipped.Status = OrderStatus.Shipped;
        orderShipped.TotalPrice = 7;

        var sales = new List<Order> {orderPending, orderCancelled, orderCompleted, orderPaid, orderShipped};

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);
        _unitOfWorkMock.Setup(u => u.Orders.GetSellerSalesAsync(userId)).ReturnsAsync(sales);

        var results = await _profileService.GetFinancesViewModelAsync(userId);

        results.PendingFunds.Should().Be(14);
        results.Transactions.Count.Should().Be(5);

    }

    [Test]
    public async Task GetFinancesViewModelAsync_ShouldMapTransactionsCorrectly()
    {
        var userId = "userId";
        var userProfile = TestDataFactory.CreateUserProfile(userId);
        var auction = TestDataFactory.CreateAuction();
        auction.Title = "Title-example";

        var order = TestDataFactory.CreateOrder(1, auction);
        order.OrderDate = DateTime.Today;
        order.TotalPrice = 111;
        order.Status = OrderStatus.Completed;
        order.Buyer.UserName = "BuyerName";

        var sales = new List<Order> {order};
        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);
        _unitOfWorkMock.Setup(u => u.Orders.GetSellerSalesAsync(userId)).ReturnsAsync(sales);

        var results = await _profileService.GetFinancesViewModelAsync(userId);

        results.Transactions[0].OrderId.Should().Be(1);
        results.Transactions[0].Date.Should().Be(DateTime.Today); 
        results.Transactions[0].Title.Should().Be("Title-example"); 
        results.Transactions[0].Amount.Should().Be(111); 
        results.Transactions[0].Status.Should().Be(OrderStatus.Completed);
        results.Transactions[0].BuyerName.Should().Be("BuyerName");
    }
    
    #endregion

    #region WithdrawFundsAsync

    [Test]
    public async Task WithdrawFundsAsync_ShouldReturnSuccessAndResetBalance_WhenDataIsCorrect()
    {
        var userId = "userid";
        var userProfile = TestDataFactory.CreateUserProfile(userId);
        userProfile.PrivateIBAN = "PL00000000000000000000000000";
        userProfile.WalletBalance = 1000;

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        var results = await _profileService.WithdrawFundsAsync(userId);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        results.Success.Should().Be(true);
        results.Message.Should().Be("Wypłacono");
        results.Amount.Should().Be(1000);
        userProfile.WalletBalance.Should().Be(0);
    }
    

    [Test]
    public async Task WithdrawFundsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        var userId = "userid";
        UserProfile userProfile = null;

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        var results = await _profileService.WithdrawFundsAsync(userId);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        results.Success.Should().Be(false);
        results.Message.Should().Be("Profil nie istnieje.");
        results.Amount.Should().Be(0);
    }

    [Test]
    public async Task WithdrawFundsAsync_ShouldReturnFalse_WhenUserHasNoIban()
    {
        var userId = "userid";
        var userProfile = TestDataFactory.CreateUserProfile(userId);
        userProfile.PrivateIBAN = null;
        userProfile.WalletBalance = 1000;

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        var results = await _profileService.WithdrawFundsAsync(userId);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        results.Success.Should().Be(false);
        results.Message.Should().Be("Brak numeru konta IBAN. Uzupełnij profil.");
        results.Amount.Should().Be(0);

    }

    [Test]
    public async Task WithdrawFundsAsync_ShouldReturnFalse_WhenBalanceIsZeroOrLess()
    {
        var userId = "userid";
        var userProfile = TestDataFactory.CreateUserProfile(userId);
        userProfile.PrivateIBAN = "PL00000000000000000000000000";
        userProfile.WalletBalance = 0;

        _unitOfWorkMock.Setup(u => u.Profiles.GetByUserIdAsync(userId)).ReturnsAsync(userProfile);

        var results = await _profileService.WithdrawFundsAsync(userId);

        _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        results.Success.Should().Be(false);
        results.Message.Should().Be("Brak środków do wypłaty.");
        results.Amount.Should().Be(0);
    }
    #endregion
}