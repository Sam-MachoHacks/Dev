namespace FamilyCluster.Common
{
    using System.Collections.Generic;

    public class Hello
    {
        public Hello( List<DataBlock> blockChain, string key)
        {
         
            this.BlockChain = blockChain;
            this.Key = key;
        }

      
        public string Key { get; private set; }
        public List<DataBlock> BlockChain {  set; get; }
    }
}