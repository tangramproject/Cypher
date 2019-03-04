using System;
namespace TangramCypher.ApplicationLayer.Wallet
{
    public class MessageTrackDto
    {
        public int Count { get; set; }
        public string PublicKey { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}
