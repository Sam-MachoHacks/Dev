using System;
using Akka.Actor;

namespace FamilyCluster.Common
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Akka.Cluster;
    using Akka.Event;
    using Akka.Util.Internal;
    using Twilio;
    using Twilio.Rest.Api.V2010.Account;

    /*
     *
     *    SendSms("wow doing this", "").Wait();
                      var allsms = ReadSms();
     */
    public class BlockProcessorActor : ReceiveActor
    {
        IBlockChainHandler BlockChain;
        public const int DefaultCreditScore = 800;

        public static ConcurrentDictionary<string, List<DataBlock>> Blocks = new ConcurrentDictionary<string, List<DataBlock>>();//<IActorRef>();


        //could be done better
        public static ConcurrentDictionary<string, IActorRef> Members = new ConcurrentDictionary<string, IActorRef>();//<IActorRef>();
        public BlockProcessorActor(IBlockChainHandler blockChain)
        {
            BlockChain = blockChain;
            //asak everyone
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), Self, new AskSelfActorIdentity(), Self);
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), Self, new CheckSms(), Self);
            
            Receive<GetBalance>(
                message =>
                {
                    var smsMessage = message.SmsMessage;
                    if (!Blocks.ContainsKey(smsMessage.From))
                    {
                        SendSms($"You have a balance of 0 with a credit score of {DefaultCreditScore}", smsMessage.From).Wait();
                    }
                    else
                    {
                        var data = Blocks[smsMessage.From].OrderByDescending(x => x.Index).FirstOrDefault();
                        SendSms($"You have a balance of {data?.Transaction?.Amount} with a credit score of {data?.Transaction?.CreditScore}", smsMessage.From).Wait();
                    }
                });

            Receive<GetBlockMessage>(
                message =>
                {
                    this.UpdateAllMembers();
                    
                });
            Receive<CheckSms>(
                message =>
                {
                    try
                    {
                        var result = ReadSms();
                        foreach (SMSMessage smsMessage in result)
                        {
                            try
                            {

                                if (smsMessage.Message.ToLower().Trim().Contains("thanks for the message"))
                                {
                                    continue;
                                }
                                var parts = smsMessage.Message.Split(' ');
                                var command = parts[0];


                                if (
                                    command.Trim().ToLower() == "payback" ||
                                    command.Trim().ToLower() == "repay" ||
                                    command.Trim().ToLower() == "credit" ||
                                    command.Trim().ToLower() == "return"
                                    )
                                {
                                    var amount = Convert.ToInt32(parts[1]);
                                    var block = CreateBlockFromSmsMessage(smsMessage, Commands.PAYBACK, amount);
                                    Blocks[smsMessage.From].Add(block);
                                    this.UpdateAllMembers();
                                    SendSms($"operation completed {command} of {amount}", smsMessage.From);
                                }
                                else if (
                                    command.Trim().ToLower() == "borrow" ||
                                    command.Trim().ToLower() == "lend" ||
                                    command.Trim().ToLower() == "take" ||
                                    command.Trim().ToLower() == "receive"
                                    )
                                {
                                    var amount = Convert.ToInt32(parts[1]);
                                    var block = CreateBlockFromSmsMessage(smsMessage, Commands.BORROW, amount);
                                    Blocks[smsMessage.From].Add(block);
                                    this.UpdateAllMembers();
                                    SendSms($"operation completed {command} of {amount}", smsMessage.From);
                                }
                                else if (command.Trim().ToLower() == "balance")
                                {
                                   Self.Forward(new GetBalance(smsMessage));
                                }
                                else
                                {
                                    throw new Exception();
                                }

                            }
                            catch (Exception e)
                            {
                                var error = "Oh oh! Please try again!";//" Try sending command like 'borrow 100' to borrow 100 dollars! :"+e.Message;
                                Console.WriteLine(error);
                                Console.WriteLine(e);
                                try
                                {
                                    SendSms(error, smsMessage.From).Wait();

                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine(exception);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

            Receive<AskSelfActorIdentity>(
                _ =>
                {

                    Context.System.ActorSelection("/user/BrotherEchoActor").Tell(new AskActorIdentity());

                });
            Receive<List<BlockChainTransferItem>>(
                blocks =>
                {
                    if (blocks.Count == 0)
                    {
                        return;
                    }
                    var hasErrors = false;
                    foreach (var block in blocks)
                    {
                        var containsBlock = Blocks.ContainsKey(block.Key);

                        if (!containsBlock)
                        {
                            break;
                        }
                        try
                        {

                            ValidateAndComputeCurrentHash(block.Key, block.BlockChain);

                        }
                        catch (Exception e)
                        {

                            Console.WriteLine("Wrong blocks sent in : " + e);
                            Debugger.Break();
                            break;
                        }

                    }

                    var updateOtherNodes = blocks.Count > Blocks.Count;

                    if (updateOtherNodes)
                    {
                        Console.WriteLine("Updating blocks ...");

                        Blocks = new ConcurrentDictionary<string, List<DataBlock>>();
                        foreach (BlockChainTransferItem blockChainTransferItem in blocks)
                        {
                            Blocks.GetOrAdd(blockChainTransferItem.Key, blockChainTransferItem.BlockChain);
                        }

                    }


                    if (updateOtherNodes)
                    {
                        UpdateAllMembers();
                    }



                    return;
                });
            Receive<Hello>(
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
            Receive<AskActorIdentity>(
                message =>
                {
                    Console.WriteLine("Asking me for address at " + Blocks.Count);
                    if (!Members.ContainsKey(Sender.ToString()))
                    {
                        Console.WriteLine(">>>Updating member " + Sender);
                        Members.GetOrAdd(Sender.ToString(), Sender);

                        var blockChainList = DictionaryToObjectClass();

                        UpdateAllMembers();
                        Sender.Tell(blockChainList);
                    }

                });
        }

        void SaveBlock(Hello hello)
        {
            this.Sender.Tell($"CREATING BLOCK CHAIN : Received message  from {this.Sender} saying ... '{hello?.BlockChain?.LastOrDefault()?.Transaction}'");

            ValidateAndComputeCurrentHash(hello.Key, hello.BlockChain);
            Blocks.GetOrAdd(hello.Key, hello.BlockChain);
            this.UpdateAllMembers();

            Console.WriteLine("[{0}]: {1}", this.Sender, hello);
        }

        static List<BlockChainTransferItem> DictionaryToObjectClass()
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
        public static List<SMSMessage> ReadSms()
        {

            const string accountSid = "<twillo sid>";
            const string authToken = "<twillo token>";

            TwilioClient.Init(accountSid, authToken);

            var messages = MessageResource.Read();

            var result = new List<SMSMessage>();
            foreach (var record in messages)
            {
                result.Add(new SMSMessage()
                {
                    From = record.From.ToString(),
                    Id = record.Sid,
                    Message = record.Body
                });
            }
            try
            {
                
                result.ForEach(r => MessageResource.Delete(r.Id));
            }
            catch (Exception e)
            {
                return new List<SMSMessage>();
            }

            return result;
        }
        void UpdateAllMembers()
        {
            var blockChainList = DictionaryToObjectClass();
            try
            {
                Members.ToList().ForEach(m => m.Value.Tell(blockChainList));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error updating all members .." + e.Message);
                Debugger.Break();
            }

        }

        public DataBlock CreateBlockFromSmsMessage(SMSMessage smsMessage, Commands command, int amount)
        {
            var Blocks = BlockProcessorActor.Blocks;

            Hello mainMessage;
            string key;
            List<DataBlock> bc = new List<DataBlock>();
            if (Blocks.Count == 0)
            {
                var newChain = new KeyValuePair<string, List<DataBlock>>(smsMessage.From, new List<DataBlock>());
                Blocks.GetOrAdd(newChain.Key, newChain.Value);
                key = smsMessage.From;
                
            }
            else
            {
                key = smsMessage.From;
               
            }
            bc = Blocks[smsMessage.From];
            DataBlock newBlock;
            var isNewBlock = bc == null || bc.Count == 0;

            //var lastBlock =  bc.OrderByDescending(x => x.Index).ToList().First();
            var lastBlock = isNewBlock ? new DataBlock(0, 100, "", "", DateTime.UtcNow.Ticks, new BlockTransaction(0, DefaultCreditScore)) :
             bc.OrderByDescending(x => x.Index).ToList().First();
            var previousAmount = lastBlock.Transaction.Amount;
            var previousScore = lastBlock.Transaction.CreditScore;
            var periodOfLendingInSec = (int)(DateTime.UtcNow - lastBlock.Transaction.DateTime).TotalSeconds;

            switch (command)
            {
                case Commands.BORROW:
                    newBlock = BlockProcessorActor.CreateNewBlock(bc, new BlockTransaction( amount+ previousAmount, previousScore - 15));
                    break;
                case Commands.PAYBACK:

                    newBlock = BlockProcessorActor.CreateNewBlock(bc, new BlockTransaction(  previousAmount- amount, previousScore - periodOfLendingInSec + 15));

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

      

        static async Task SendSms(string msg, string to)
        {
            if (to == "+17784005245")
            {
                return;
            }
            // Find your Account Sid and Token at twilio.com/console
            const string accountSid = "<twillo sid>";
            const string authToken = "<twillo token>";

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: msg,
                from: new Twilio.Types.PhoneNumber("+17784005245"),
                to: new Twilio.Types.PhoneNumber(to)
            );

            Console.WriteLine(message.Sid);
        }
    }

    public class GetBalance
    {
        public GetBalance(SMSMessage smsMessage)
        {
            this.SmsMessage = smsMessage;
        }

        public SMSMessage SmsMessage { get; private set; }
    }

    public enum Commands
    {
        BORROW,
        PAYBACK
    }

    public class CheckSms
    {
        public CheckSms()
        {
            Messages=new List<SMSMessage>();
        }
        public List<SMSMessage> Messages { set; get; }
    }

    public class SMSMessage
    {
        public string From { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }
    }

    public class BlockChainTransferItem
    {
        public BlockChainTransferItem()
        {
            BlockChain = new List<DataBlock>();
        }
        public string Key { set; get; }
        public List<DataBlock> BlockChain { set; get; }
    }

    public class AskSelfActorIdentity
    {
    }

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

    public class BlockTransaction
    {
        public BlockTransaction(int amount, int creditScore)
        {
            this.Amount = amount;
            this.CreditScore = creditScore;
            DateTime = DateTime.UtcNow;
        }

        public int Amount { private set; get; }
        public int CreditScore { private set; get; }
        public DateTime DateTime { private set; get; }
    }

    public class AskActorIdentity
    {

    }
    public class GetBlockMessage { }
}