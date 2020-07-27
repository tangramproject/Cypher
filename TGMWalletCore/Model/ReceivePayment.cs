// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.ComponentModel.DataAnnotations;

namespace TGMWalletCore.Model
{
    public class ReceivePayment
    {
        public Credentials  Credentials { get; set; }
        [Required]
        public string FromAddress { get; set; }
        public Message  RedemptionMessage { get; set; }
    }
}
