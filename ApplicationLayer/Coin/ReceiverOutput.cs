namespace TangramCypher.ApplicationLayer.Coin
{
    public class ReceiverOutput
    {
        public double Amount { get; private set; }
        public byte[] Commit { get; private set; }
        public byte[] Blind { get; private set; }

        public ReceiverOutput(double amount, byte[] commit, byte[] blind)
        {
            Amount = amount;
            Commit = commit;
            Blind = blind;
        }
    }
}
