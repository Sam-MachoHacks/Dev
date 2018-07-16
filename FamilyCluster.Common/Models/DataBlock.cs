namespace FamilyCluster.Common
{
    public class DataBlock
    {
        public DataBlock(int index, int proof, string previousHash, string currentHash, long timeStamp, BlockTransaction transactions)
        {
            this.Index = index;
            this.Proof = proof;
            this.PreviousHash = previousHash;
            this.CurrentHash = currentHash;
            this.TimeStamp = timeStamp;
            this.Transaction = transactions;
        }

        public int Index { private set; get; }
        public int Proof { private set; get; }
        public string PreviousHash { private set; get; }
        public string CurrentHash { private set; get; }
        public long TimeStamp { private set; get; }
        public BlockTransaction Transaction { private set; get; }
    }
}