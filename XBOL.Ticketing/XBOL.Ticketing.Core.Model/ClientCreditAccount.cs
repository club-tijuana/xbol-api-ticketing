namespace XBOL.Ticketing.Core.Model
{
    public class ClientCreditAccount : BaseModel
    {
        public long ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public decimal CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<ClientCreditTransaction> ClientCreditTransactions { get; set; } = [];
    }
}