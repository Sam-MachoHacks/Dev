namespace FamilyCluster.Common
{
    public class GetBalanceMessage
    {
        public GetBalanceMessage(SMSMessage smsMessage)
        {
            this.SmsMessage = smsMessage;
        }

        public SMSMessage SmsMessage { get; private set; }
    }
}