using System;
using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Coin
{
    public class EnvelopeDto
    {
        public string Commitment { get; set; }
        public string Proof { get; set; }
        public string PublicKey { get; set; }
        public string Signature { get; set; }
    }
}
