namespace FamilyCluster.Common.Models
{
    using System.Collections.Generic;

    public class BlockChainEntity
    {
        public BlockChainEntity(List<DataBlock> blockChain, string key)
        {
            this.BlockChain = blockChain;
            this.Key = key;
        }

        public string Key { get; private set; }
        public List<DataBlock> BlockChain { set; get; }
    }
}