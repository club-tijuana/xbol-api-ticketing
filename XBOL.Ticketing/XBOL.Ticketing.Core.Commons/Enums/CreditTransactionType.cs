namespace XBOL.Ticketing.Core.Commons.Enums
{
    public enum CreditTransactionType
    {
        // Debits (increase teh Balance Owed)
        Drawdown,   // Standard charge

        Interest,
        Fee,
        AdjustmentDebit, // To avoid deleting or updating records

        // Credits (Decrease the Balance Owed)
        Payment,

        Reversal,
        AdjustmentCredit,
    }
}
