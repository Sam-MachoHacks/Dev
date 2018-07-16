namespace FamilyCluster.Common
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public static class AppExtensions
    {
        public static List<BlockChainTransferItem> ToBlockChainTransferItems(this ConcurrentDictionary<string, List<DataBlock>> Blocks)
        {
            var blockChainList = new List<BlockChainTransferItem>();
            foreach (KeyValuePair<string, List<DataBlock>> keyValuePair in Blocks)
            {
                blockChainList.Add(
                    new BlockChainTransferItem()
                    {
                        Key = keyValuePair.Key,
                        BlockChain = keyValuePair.Value
                    });
            }

            return blockChainList;
        }
    }
}