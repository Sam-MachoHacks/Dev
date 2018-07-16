namespace FamilyCluster.Common
{
    public class SMSMessage
    {
        public SMSMessage(string id, string message, string @from, int amount, string command)
        {
            this.Id = id;
            this.Message = message;
            this.From = @from;
            this.Amount = amount;
            this.Command = command;
        }

        public string Command { get; private set; }
        public int Amount { get; private set; }

        public string From { get; private set; }

        public string Id { get; private set; }

        public string Message { get; private set; }
    }
}