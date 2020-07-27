// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace TGMWalletCore.Model
{
    [ProtoContract]
    public class Block
    {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public Coin Coin { get; set; }
        [ProtoMember(3)]
        public string PublicKey { get; set; }
        [ProtoMember(4)]
        public string Signature { get; set; }
    }
}
