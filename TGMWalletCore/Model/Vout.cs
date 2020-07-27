// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace TGMWalletCore.Model
{
    [ProtoContract]
    public class Vout
    {
        [ProtoMember(1)]
        public string[] C { get; set; }
        [ProtoMember(2)]
        public string[] E { get; set; }
        [ProtoMember(3)]
        public string[] N { get; set; }
        [ProtoMember(4)]
        public string[] P { get; set; }
        [ProtoMember(5)]
        public string[] R { get; set; }

        public Vout(int size)
        {
            C = new string[size];
            E = new string[size];
            N = new string[size];
            P = new string[size];
            R = new string[size];
        }
    }
}
