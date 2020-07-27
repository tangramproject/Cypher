// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Model
{
    public class SendPayment
    {
        public Credentials  Credentials { get; set; }
        public double Amount { get; set; }
        public string Address { get; set; }
        public bool CreateRedemptionKey { get; set; }
        public string Memo { get; set; }
    }
}
