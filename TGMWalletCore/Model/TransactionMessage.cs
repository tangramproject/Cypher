// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Model
{
    public class TransactionMessage
    {
        public ulong Amount { get; set; }
        public string Blind { get; set; }
    }
}
