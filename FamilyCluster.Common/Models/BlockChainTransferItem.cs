namespace FamilyCluster.Common
{
    using System.Collections.Generic;

    public class BlockChainTransferItem
    {
        public BlockChainTransferItem()
        {
            this.BlockChain = new List<DataBlock>();
        }

        public string Key { set; get; }
        public List<DataBlock> BlockChain { set; get; }
    }
}