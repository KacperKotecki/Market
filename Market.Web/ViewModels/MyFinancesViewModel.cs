using Market.Web.Models;

namespace Market.Web.ViewModels
{
    public class MyFinancesViewModel
    {
        public string IBAN { get; set; } = string.Empty;
        public bool HasIBAN => !string.IsNullOrEmpty(IBAN);

        public decimal AvailableFunds { get; set; }
        public decimal PendingFunds { get; set; }  
        public List<FinanceTransactionDto> Transactions { get; set; } = new();
    }

    public class FinanceTransactionDto
    {
        public int OrderId { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public OrderStatus Status { get; set; }
        public string BuyerName { get; set; } = string.Empty;
    }
}