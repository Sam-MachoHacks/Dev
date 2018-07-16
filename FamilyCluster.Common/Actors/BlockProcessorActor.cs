using Akka.Actor;
using System;

namespace FamilyCluster.Common
{
    using FamilyCluster.Common.Models;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public class BlockProcessorActor : ReceiveActor
    {
        private IBlockChainHandler BlockChain;
        public const int DefaultCreditScore = 800;

        public static ConcurrentDictionary<string, List<DataBlock>> Blocks = new ConcurrentDictionary<string, List<DataBlock>>();//<IActorRef>();

        public BlockProcessorActor(IBlockChainHandler blockChain)
        {
            BlockChain = blockChain;

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), Self, new CheckSmsMessage(), Self);

            Receive<GetBalanceMessage>(
                message =>
                {
                    Console.WriteLine("checking sms msg ...");
                    var smsMessage = message.SmsMessage;
                    if (!Blocks.ContainsKey(smsMessage.From))
                    {
                        SmsHandler.SendSms($"You have a balance of 0 with a credit score of {DefaultCreditScore}", smsMessage.From).Wait();
                    }
                    else
                    {
                        var data = Blocks[smsMessage.From].OrderByDescending(x => x.Index).FirstOrDefault();
                        SmsHandler.SendSms($"You have a balance of {data?.Transaction?.Amount} with a credit score of {data?.Transaction?.CreditScore}", smsMessage.From).Wait();
                    }
                });

            Receive<PerformUpdateMessage>(
                message =>
                {
                    var smsMessage = message.SmsMessage;
                    try
                    {
                        var command = smsMessage.Command;// parts[0];
                        var amount = smsMessage.Amount;// Convert.ToInt32(parts[1]);

                        DataBlock block;
                        if (
                            command == "payback" ||
                            command == "repay" ||
                            command == "credit" ||
                            command == "return"
                            )
                        {
                            block = CreateBlockFromSmsMessage(smsMessage, Commands.PAYBACK, amount);
                        }
                        else if (
                            command == "borrow" ||
                            command == "lend" ||
                            command == "take" ||
                            command == "receive"
                            )
                        {
                            block = CreateBlockFromSmsMessage(smsMessage, Commands.BORROW, amount);
                        }
                        else
                        {
                            throw new Exception();
                        }

                        if (Blocks.ContainsKey(smsMessage.From))
                        {
                            Blocks[smsMessage.From].Add(block);
                        }
                        else
                        {
                            Blocks.GetOrAdd(smsMessage.From, new List<DataBlock>()
                            {
                                block
                            });
                        }

                        this.UpdateAllMembers(Blocks);
                        SmsHandler.SendSms($"operation completed {command} of {amount}", smsMessage.From).Wait();
                    }
                    catch (Exception e)
                    {
                        var error = "Oh oh! Please try again! Try sending command like 'borrow 100' to borrow 100 dollars! :" + e.Message;
                        Console.WriteLine(error);
                        Console.WriteLine(e);
                        Console.WriteLine($"original message {message.SmsMessage?.Message} with command {message.SmsMessage?.Command}");

                        SmsHandler.SendSms(error, smsMessage.From).Wait();
                    }
                });
            Receive<CheckSmsMessage>(
                message =>
                {
                    var result = SmsHandler.ReadSms();
                    foreach (SMSMessage smsMessage in result)
                    {
                        try
                        {
                            var command = smsMessage.Command;

                            if (command.Trim().ToLower() == "balance")
                            {
                                Self.Forward(new GetBalanceMessage(smsMessage));
                            }
                            else
                            {
                                Self.Forward(new PerformUpdateMessage(smsMessage));
                            }
                        }
                        catch (Exception e)
                        {
                            var error = "Oh oh! cant read sms";//" Try sending command like 'borrow 100' to borrow 100 dollars! :"+e.Message;
                            Console.WriteLine(error);
                            Console.WriteLine(e);
                        }
                    }
                });

            Receive<List<BlockChainTransferItem>>(
                blocks =>
                {
                    if (blocks.Count == 0)
                    {
                        return;
                    }

                    foreach (var block in blocks)
                    {
                        try
                        {
                            ValidateAndComputeCurrentHash(block.Key, block.BlockChain);
                            var containsBlock = Blocks.ContainsKey(block.Key);

                            if (!containsBlock)
                            {
                                Console.WriteLine("Adding new data to block");
                                Blocks.GetOrAdd(block.Key, block.BlockChain);
                            }
                            else
                            {
                                Console.WriteLine("modifying existing data");
                                Blocks[block.Key] = block.BlockChain;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unable to update blockchain");
                            Console.WriteLine(e);
                        }
                    }
                });
            Receive<BlockChainEntity>(
                hello =>
                {
                    try
                    {
                        this.SaveBlock(hello);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to create or update blockchain");
                        Console.WriteLine(e);
                        Debugger.Break();
                    }
                });
        }

        private void SaveBlock(BlockChainEntity blockChainEntity)
        {
            this.Sender.Tell($"CREATING BLOCK CHAIN : Received message  from {this.Sender} saying ... '{blockChainEntity?.BlockChain?.LastOrDefault()?.Transaction}'");

            ValidateAndComputeCurrentHash(blockChainEntity.Key, blockChainEntity.BlockChain);
            Blocks.GetOrAdd(blockChainEntity.Key, blockChainEntity.BlockChain);
            this.UpdateAllMembers(Blocks);

            Console.WriteLine("[{0}]: {1}", this.Sender, blockChainEntity);
        }

        private void UpdateAllMembers(ConcurrentDictionary<string, List<DataBlock>> Blocks)
        {
            var blockChainList = Blocks.ToBlockChainTransferItems();
            try
            {
                Context.System.ActorSelection("/user/BrotherEchoActor").Tell(blockChainList);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error updating all members .." + e.Message);
                Debugger.Break();
            }
        }

        public DataBlock CreateBlockFromSmsMessage(SMSMessage smsMessage, Commands command, int amount)
        {
            List<DataBlock> bc = new List<DataBlock>();

            bc = Blocks.ContainsKey(smsMessage.From) ? Blocks[smsMessage.From] : new List<DataBlock>();
            DataBlock newBlock;
            var isNewBlock = bc == null || bc.Count == 0;

            var lastBlock = isNewBlock ? new DataBlock(0, 100, "", "", DateTime.UtcNow.Ticks, new BlockTransaction(0, DefaultCreditScore)) :
             bc.OrderByDescending(x => x.Index).ToList().First();
            var previousAmount = lastBlock.Transaction.Amount;
            var previousScore = lastBlock.Transaction.CreditScore;
            var periodOfLendingInSec = (int)(DateTime.UtcNow - lastBlock.Transaction.DateTime).TotalSeconds;

            switch (command)
            {
                case Commands.BORROW:
                    newBlock = BlockProcessorActor.CreateNewBlock(bc, new BlockTransaction(amount + previousAmount, previousScore - 15));
                    break;

                case Commands.PAYBACK:

                    newBlock = BlockProcessorActor.CreateNewBlock(bc, new BlockTransaction(previousAmount - amount, previousScore - periodOfLendingInSec + 15));

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }

            return newBlock;
        }

        public static DataBlock CreateNewBlock(List<DataBlock> chain, BlockTransaction data)
        {
            var isNewBlock = chain == null || chain.Count == 0;
            var sortedBlocks = chain.OrderByDescending(x => x.Index).ToList();
            var timeStamp = DateTime.UtcNow.Ticks;
            var lastBlock = isNewBlock ? new DataBlock(0, 100, "", "", timeStamp, new BlockTransaction(0, DefaultCreditScore)) :
                sortedBlocks[0];
            string previousHash = lastBlock.CurrentHash;
            var newIndex = lastBlock.Index + 1;

            var transaction = data;
            var newHash = Sha256(newIndex + previousHash + timeStamp + transaction);
            var newBlock = new DataBlock(newIndex, 100, previousHash, newHash, timeStamp, transaction);
            return newBlock;
        }

        public static string Sha256(string randomString)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string ValidateAndComputeCurrentHash(string key, List<DataBlock> block)
        {
            //var lengthIncreased = block.Count > Blocks[key].Count;
            var sortedBlocks = block.OrderByDescending(x => x.Index).ToList();

            if (block.Count == 0)
            {
                return "";
            }
            var lastBlock = sortedBlocks[0];
            if (block.Count == 1)
            {
                var dataBlock = block.First();
                if (dataBlock.PreviousHash != "" || dataBlock.Index != 1)
                {
                    throw new Exception("Invalid block chain with one entry");
                }
                else
                {
                    return dataBlock.CurrentHash;
                }
            }
            else
            {
                var previousBlock = sortedBlocks[1];
                var hashMatches = lastBlock.PreviousHash == previousBlock.CurrentHash;
                var indexMatches = lastBlock.Index == previousBlock.Index + 1;
                var lastBlockHash = Sha256(lastBlock.Index + lastBlock.PreviousHash + lastBlock.TimeStamp + lastBlock.Transaction);
                var hashIsCorrect = lastBlockHash == lastBlock.CurrentHash;
                if (hashMatches && indexMatches && hashIsCorrect)
                {
                    return lastBlockHash;
                }
                else
                {
                    throw new Exception("Invalid Block");
                }
            }
        }
    }

    public class PerformUpdateMessage
    {
        public PerformUpdateMessage(SMSMessage smsMessage)
        {
            this.SmsMessage = smsMessage;
        }

        public SMSMessage SmsMessage { get; private set; }
    }
}