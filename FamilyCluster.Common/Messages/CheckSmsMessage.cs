namespace FamilyCluster.Common
{
    using System.Collections.Generic;

    public class CheckSmsMessage
    {
        public CheckSmsMessage()
        {
            this.Messages = new List<SMSMessage>();
        }

        public List<SMSMessage> Messages { set; get; }
    }
}