namespace FamilyCluster.Common
{
    using System;

    public class BlockTransaction
    {
        public BlockTransaction(int amount, int creditScore)
        {
            this.Amount = amount;
            this.CreditScore = creditScore;
            this.DateTime = DateTime.UtcNow;
        }

        public int Amount { private set; get; }
        public int CreditScore { private set; get; }
        public DateTime DateTime { private set; get; }
    }
}