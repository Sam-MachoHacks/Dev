namespace FamilyCluster.Brother
{
    using Akka.Actor;
    using Akka.Configuration;
    using Akka.Routing;
    using FamilyCluster.Common;
    using FamilyCluster.Common.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ConsoleClient
    {
        public static void StartMain(string Id)
        {
            Id += Guid.NewGuid().ToString().Replace("-", "");
            try
            {
                Console.WriteLine("Starting " + Id + " ...");
                using (var system = ActorSystem.Create("FamilyCluster", ConfigurationFactory.ParseString(AppConfiguration.GetAkkaConfiguration(Id))))
                {
                    var actor = system.ActorOf(Props.Create(() => new BlockProcessorActor(new BlockChainHandler())).WithRouter(FromConfig.Instance), Id);

                    while (true)
                    {
                        var message = Console.ReadLine();

                        var Blocks = BlockProcessorActor.Blocks;

                        BlockChainEntity mainMessage;
                        string key;
                        List<DataBlock> blockChain;
                        if (Blocks.Count == 0)
                        {
                            var newChain = new KeyValuePair<string, List<DataBlock>>(Guid.NewGuid().ToString(), new List<DataBlock>());
                            Blocks.GetOrAdd(newChain.Key, newChain.Value);

                            key = Guid.NewGuid().ToString();
                            blockChain = new List<DataBlock>();
                        }
                        else
                        {
                            var blockChainData = Blocks.First();
                            key = blockChainData.Key;
                            blockChain = blockChainData.Value;
                        }

                        var newBlock = BlockProcessorActor.CreateNewBlock(blockChain, new BlockTransaction(100, BlockProcessorActor.DefaultCreditScore));

                        blockChain.Add(newBlock);

                        mainMessage = new BlockChainEntity(blockChain, key);
                        var result = actor.Ask<string>(mainMessage).Result;
                        Console.WriteLine($"RESULT FIRST : {result}");
                        Console.WriteLine($"CURRENT BLOCK : {Blocks.Count}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();

                throw;
            }
        }
    }
}