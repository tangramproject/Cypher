// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace TGMWalletCore.Model
{
    [ProtoContract]
    public class Vin
    {
        [ProtoMember(1)]
        public string K { get; set; }
        [ProtoMember(2)]
        public string M { get; set; }
        [ProtoMember(3)]
        public string P { get; set; }
        [ProtoMember(4)]
        public string S { get; set; }
    }
}
