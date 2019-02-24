using System;
namespace TangramCypher.ApplicationLayer.Coin
{
    public class CoinDto
    {
        public EnvelopeDto Envelope { get; set; }
        public string Hash { get; set; }
        public string Hint { get; set; }
        public string Keeper { get; set; }
        public string Principle { get; set; }
        public string Stamp { get; set; }
        public int Version { get; set; }
    }
}
