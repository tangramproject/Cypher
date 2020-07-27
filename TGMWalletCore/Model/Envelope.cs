// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace TGMWalletCore.Model
{
    [ProtoContract]
    public class Envelope
    {
        [ProtoMember(1)]
        public string Commitment { get; set; }
        [ProtoMember(2)]
        public string Proof { get; set; }
        [ProtoMember(3)]
        public string PublicKey { get; set; }
        [ProtoMember(4)]
        public string Signature { get; set; }
        [ProtoMember(5)]
        public string RangeProof { get; set; }
    }
}
