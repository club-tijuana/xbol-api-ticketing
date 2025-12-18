using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class ClientCreditTransaction
    {
        public long Id { get; set; }

        public long ClientCreditAccountId { get; set; }
        public ClientCreditAccount ClientCreditAccount { get; set; } = null!;

        public decimal Amount { get; set; }
        public CreditTransactionType TransactionType { get; set; }
        public string Description { get; set; } = null!;
        public string Details { get; set; } = null!; // JSON
        public Guid Reference { get; set; }

        public CreditTransactionStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
    }
}