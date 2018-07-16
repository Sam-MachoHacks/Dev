using System;
using System.Configuration;
using Akka.Actor;
using Akka.Routing;
using FamilyCluster.Common;

namespace FamilyCluster.Sister
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Akka.Configuration;
    using Twilio;
    using Twilio.Rest.Api.V2010.Account;

    internal class Program
    {
        static string id = "SisterEchoActor";

        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting SisterSystem ...");
                using (var system = ActorSystem.Create("FamilyCluster", ConfigurationFactory.ParseString(AppConfiguration.GetAkkaConfiguration(id))))
                {
                    var brotherEchoActor = system.ActorOf(Props.Create(() => new BlockProcessorActor(new BlockChainHandler())).WithRouter(FromConfig.Instance), id);

                    while (true)
                    {
                       //brotherEchoActor.Ask<string>(
                       //     new CheckSms()
                       //     {
                       //         Messages = ReadSms()
                       //     });
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
                var newDelay = new Random().Next(0, 5);
                Task.Delay(TimeSpan.FromSeconds(newDelay)).Wait();
                result.ForEach(r => MessageResource.Delete(r.Id));
            }
            catch (Exception e)
            {
                return new List<SMSMessage>();
            }

            return result;
        }

    }
}