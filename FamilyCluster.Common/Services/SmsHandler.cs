namespace FamilyCluster.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Twilio;
    using Twilio.Rest.Api.V2010.Account;
    using Twilio.Types;

    public class SmsHandler
    {
        private const string accountSid = "";
        private const string authToken = "";
        const string Number = "";
        public static List<SMSMessage> ReadSms()
        {
            try
            {
                TwilioClient.Init(accountSid, authToken);

                var messages = MessageResource.Read();
                var result = new List<SMSMessage>();
                foreach (var record in messages)
                {
                    if (record.Body.ToLower().Trim().Contains("thanks for the message") ||
                        record.Body.ToLower().Trim().Contains("Oh") ||
                        record.Body.ToLower().Trim().Contains("system:"))
                    {
                        try
                        {
                            MessageResource.Delete(record.Sid);
                            Console.WriteLine("Deleted junk! Yay!");
                        }
                        catch (Exception e)
                        {
                        }

                        continue;
                    }
                    else
                    {
                        MessageResource.Delete(record.Sid);
                    }

                    var parts = record.Body.Split(' ');
                    var command = (parts.Length > 0 ? parts[0] : "").Trim().ToLower();
                    var tmpAmount = parts.Length > 1 ? parts[1] : "0";
                    int amount;
                    Int32.TryParse(tmpAmount, out amount);
                    result.Add(new SMSMessage(record.Sid, record.Body, record.From.ToString(), amount, command));
                }
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Didn't win the race : " + e.Message);
                return new List<SMSMessage>();
            }
        }

        public static async Task SendSms(string msg, string to)
        {
            try
            {
                if (to == Number)
                {
                    return;
                }
                // Find your Account Sid and Token at twilio.com/console
                TwilioClient.Init(accountSid, authToken);

                var message = await MessageResource.CreateAsync(
                    body: "SYSTEM:" + msg,
                    @from: new PhoneNumber(Number),
                    to: new PhoneNumber(to)
                );

                Console.WriteLine(message.Sid);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error sending sms...");
                Console.WriteLine(e);
            }
        }
    }
}